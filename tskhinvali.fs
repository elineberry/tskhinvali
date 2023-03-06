require ./lib/random-util.fs
require ./lib/etch.fs
require ./lib/util.fs
require ./lib/messages.fs
require ./lib/debug.fs

\ line 0 -- msg
\ line 1 -- map start
\ line 22 -- map end
\ line 23 -- status

\ ### CONSTANTS ###
: $version s" 1.0" ;
: $title s" Tskhinvali" ;
0 constant msg-line
23 constant status-line
1 constant map-y-offset
80 constant map-width
22 constant map-height
map-width map-height * constant map-size
here map-size allot constant map
here map-size allot constant visible
here map-size allot constant fov
\ here map-size units allot constant unit-array
\ here map-size items allot constant item-array
here map-size allot constant unit-move-queue


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

begin-structure house
	field: house.seed 
	field: floors
end-structure 

begin-structure room
	field: r.x
	field: r.y
	field: r.w
	field: r.h
	field: r.type
end-structure
: rooms room * ;

begin-structure door
	field: d.x
	field: d.y
	field: d.type
end-structure
: doors door * ;

\ ### LEVEL GENERATION

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
0 value forest-level
0 value dread 	\ How much the forest wants to kill you
0 value rogue.x
0 value rogue.y
8 value rogue.max-hp
rogue.max-hp value rogue.hp
100 value rogue.food
8 value rogue.strength
0 value forest-level-seed
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
	." s    -- show story" cr
	." q    -- quit game" CR
	cr .press-key-prompt ;

: .story
	page $title .formatted-title space $version type
	cr cr
	." King Torshavn is dying." cr
	cr
	." A sickness fouls his blood and he grows weaker every day. " cr
	." You must carry the life saving medicinedrug through the " cr
	." fae forest and deliver it to the palace before the king" cr
	." perishes and the land is plunged into chaos or democracy." cr
	cr
	." Journey lightly and swift. Do not disturb the peace of the" cr
	." forest or the creatures within!" cr
	cr
	." Wild mushrooms will sustain you and draw creatures away from" cr
	." you when dropped. Use them wisely!"
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
			[char] s of .story endof
			[char] Z of debug if wizard? 0= to wizard? then endof
		endcase
		;


\ ### GAME LOOP ###
false value is-dead?
: process-death ;
: input-loop ;
: post-turn-actions ;
: update-fov ;
: update-ui ;
: game-init true to is-playing? ;
: game-loop
	hide-cursor
	util:set-colors
	game-init
	util:set-colors
	.story
	.help
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


