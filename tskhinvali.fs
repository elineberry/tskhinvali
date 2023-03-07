require ./lib/random-util.fs
require ./lib/etch.fs
require ./lib/util.fs
require ./lib/messages.fs
require ./lib/debug.fs
require intro.fs

\ ### STRUCTS ###
begin-structure unit
	field: u.char
	field: u.color
	field: u.activated
	field: u.attack
	field: u.damage
	field: u.hp
end-structure
: units unit * ;

begin-structure item
	field: i.char
	field: i.color
	field: i.type
	field: i.power
end-structure
: items item * ;

begin-structure house
	field: house.seed 
	field: house.floors
end-structure 

begin-structure room
	field: r.x
	field: r.y
	field: r.w
	field: r.h
	field: r.z
	field: r.lit
end-structure
: rooms room * ;

begin-structure door
	field: d.x
	field: d.y
	field: d.type
end-structure
: doors door * ;

\ ### CONSTANTS ###
: $version s" 1.0" ;
: $title s" Tskhinvali" ;
0 constant msg-line
23 constant vibe-line
24 constant status-line
1 constant map-y-offset
80 constant map-width
21 constant map-height
here 100 cells allot constant room-array
map-width map-height * constant map-size
map-size 5 * constant house-levels
here house-levels allot constant map
here house-levels allot constant visible
here house-levels allot constant fov
here house-levels units allot constant unit-array
here house-levels items allot constant item-array
here house-levels allot constant unit-move-queue

\ ### LEVEL GENERATION
\ I think I can just generate this once and then transmogrify as time passes
\ So it's one giant level that changes over time
\ Maybe each room has one or two doors and they disappear and appear
\ If an enemy is inaccessible, you can scream to summon all units
\ Start with the full map, then just one or two floors
\ stair case is an item, one for each floor, maybe
\ feel safe in the basement
\ it harms friendlies everytime you enter a room with one in them
\ they can never fall below 0, but friendlies at 0 turn next awakening
\ oh shit, it's even easier than that. I just have an array of rooms which I 
\ build once
\ create units in ramdom rooms 
\ just have to link the staircases, or have them go to some random place
\ doors are just units, they only appear in the room you're in or if it's vis
: center-x map-width 2 / ;
: center-y map-height 2 / ;

: basement ( -- room )	\ this should only be called once
	here room allot >r	
	10 20 random-in-range r@ r.w !
	5 10 random-in-range r@ r.h !
	center-x r@ r.w @ 2 / - r@ r.x !
	center-y r@ r.h @ 2 / - r@ r.y !
	0 r@ r.z !
	r> ;
	
: random-room ( -- room )
	here room allot >r 
	5 10 random-in-range r@ r.x !
	2 5  random-in-range r@ r.y !
	40 65 random-in-range r@ r.w !
	12 15 random-in-range r@ r.h !
	r> ;

: .room { room -- }
	room r.x @
	room r.y @
	room r.w @
	room r.h @
	box ;	

: empty-room ( -- room ) here room allot ;

: split-room { room -- room room1 }
	empty-room >r
	room r.w @ 2 / room r.w !
	room r.w @ room r.x @ + r@ r.x !
	room r.h @ r@ r.h !
	room r.y @ r@ r.y !
	room r.w @ r@ r.w ! 
	room r> ;

\ ### STATE ###
0 value turn
0 value awakening
0 value pos-vibes
0 value neg-vibes
0 value score

0 value rogue.x
0 value rogue.y
0 value rogue.z		\ the floor of the house we're on
100 value rogue.presence
1 value rogue.level
0 value rogue.exp

\ frighteners
0 value rogue.chill
0 value rogue.scream
0 value rogue.poltergeist
0 value rogue.apparate
0 value rogue.breath

7 value fov-distance

false value is-playing?
false value do-turn?
false value wizard?


\ ### UI ###
: centered-x ( u -- x ) map-width swap - 2 / ;
: .centered ( addr u -- ) dup centered-x 1 at-xy type ;
: .formatted-title ( addr u -- )
	tty-inverse .centered reset-colors ;
: .press-key-prompt s" -- press any key to continue --"
	dup centered-x map-height at-xy type key drop ;

\ ### STATUS LINE ###
: status-line-y map-height 1+ ;
: .status-line-bg pad 80 bl fill 0 status-line-y at-xy pad 80 type ;
\ TODO make a vibeline
: .ordinal { n -- }		\ Handles ordinals from 0 to 10, and 20+
	n 10 mod
	case
		1 of s" st" endof
		2 of s" nd" endof
		3 of s" rd" endof 
		>r s" th" r>
	endcase n 0 .r type ;
: .ordinal-teen { n -- }	\ Handles ordinals in the teens
	 s" th" n 0 .r type ;
: .ordinal { n -- } 
	10 n < 20 n > and if n .ordinal-teen else n .ordinal then ;
: .status-line 
	tty-inverse
	.status-line-bg
	0 map-height 1+ at-xy
	awakening .ordinal ."  awakening" .tab
	." turn: " turn 3 .r .tab
	." +vibes: " pos-vibes 3 .r .tab
	." -vibes: " neg-vibes 3 .r .tab 
	." score: " score .
	tty-reset ;
: bounds-check ;
: n-to-xy ( n -- x y ) bounds-check map-width /mod ;
: xy-to-n ( x y -- n ) map-width * + bounds-check ;
: rogue.n rogue.x rogue.y xy-to-n ;
: .debug-line
	debug if 
	0 map-height 2 + at-xy
	." location: " rogue.n 4 .r ." :" rogue.x 2 .r ." ," rogue.y 2 .r 
	.tab ." here: " here 12 .r
	.tab ." depth: " depth . then ;


: .help
	page $title .formatted-title space $version type
	CR CR
	."    Map Items " cr cr
	." letter -- enemy" cr
	." }{~    -- forest" cr
	." %      -- wild mushroom" cr
	." >      -- level exit" cr
	." $      -- final exit" cr
	cr
	."   Commands " cr cr
	." hjkl -- movement"  CR
	." yubn -- diagonal movement"  CR
	CR
	." ?    -- help (this screen)" cr
	." i    -- show inventory" cr
	." e    -- eat a mushroom" cr
	." d    -- drop an item" cr
	." m    -- message list" CR
	." q    -- quit game" CR
	cr .press-key-prompt ;

: eat-something ; \ might be fun for a ghost ;(

: show-message-history
	page s" Message History" .formatted-title
	cr cr
	msg:show-full
	cr .press-key-prompt ;

\ ### MOVEMENT ###
: validate-position ; 	\ no idea what this does if it's invalid
: validate-move 2drop true ;
: move-rogue.x { x-offset }
    rogue.x x-offset + to rogue.x ;
: move-rogue.y { y-offset }
    rogue.y y-offset + to rogue.y ;
: move-rogue { x-offset y-offset } 
		true to do-turn?
    x-offset y-offset validate-move if
    x-offset move-rogue.x
    y-offset move-rogue.y 
		\ 'post-move-actions
		then validate-position ;
: d-left -1 0 ;
: d-right 1 0 ;
: d-up 0 -1 ;
: d-down 0 1 ;
: d-left-up -1 -1 ;
: d-left-down -1 1 ;
: d-right-up 1 -1 ;
: d-right-down 1 1 ;
: run-rogue ;		\ not implemented


\ ### DO COMMAND ###
: do-command
	key
		msg:next-line \ This is where we clear the messages from last turn.
		case
			[char] h of d-left move-rogue endof
			[char] j of d-down move-rogue endof
			[char] k of d-up move-rogue endof
			[char] l of d-right move-rogue endof
			[char] y of d-left-up move-rogue endof
			[char] u of d-right-up move-rogue endof
			[char] b of d-left-down move-rogue endof
			[char] n of d-right-down move-rogue endof
			[char] H of d-left run-rogue endof
			[char] L of d-right run-rogue endof
			[char] J of d-down run-rogue endof
			[char] K of d-up run-rogue endof
			[char] Y of d-left-up run-rogue endof
			[char] U of d-right-up run-rogue endof
			[char] B of d-left-down run-rogue endof
			[char] N of d-right-down run-rogue endof

			[char] . of true to do-turn? endof
			[char] e of eat-something endof
			[char] m of show-message-history endof
			[char] q of s" Really quit?" toast [char] y <> to is-playing? endof
			[char] ? of .help endof
			[char] Z of debug if wizard? 0= to wizard? then endof
		endcase
		;

\ ### MAP DRAWING ###
\ loop through the rooms array
\ if the room is on this level and is lit, draw it
\ i got confused and did character access first ha
: room@ ( n -- addr ) cells room-array + @ ;
: room!! ( addr n -- ) cells room-array + ! ;
: room! ( addr -- ) \ stores a room addr with no checks or balances
	100 0 do
		i room@ 0= if 
			i room!! unloop exit 
		then
	loop ;
: .map 
	100 0 do
		i room@ 0<> if
			i room@ dup r.z @ rogue.z = if .room else drop then
		then
	loop
;


\ ### GAME LOOP ###
false value is-dead?
: process-death ;
: input-loop ;
: post-turn-actions ;
: update-fov ;
: .message-line ;
: .items ;
: .units ;
: .rogue ;
: update-ui .debug-line .status-line .message-line .map .items .units .rogue ;
: update-ui'
	\ write the updated map data to the map buffer
	\ redraw the status bar
	.status-line
	\ tell the map to redraw itself
;
: game-init 
	1 to awakening 
	\ clear map, items, units arrays
	\ build house
	\ set rogue location
	basement room!
	true to is-playing? ;
: game-loop
	hide-cursor
	\ .intro						\ I want to use input from this in the game
	\ util:set-colors
	game-init
	\ util:set-colors
	\ .help
	begin
		false to do-turn?
		util:set-colors
		update-fov
		update-ui
		input-loop
		do-command
		do-turn? if post-turn-actions then
		is-dead? if process-death then
		is-playing? 0=
	until
  show-cursor ;

page
game-loop
