require ./lib/random-util.fs
require ./lib/ttester.fs

begin-structure room
	field: r.x
	field: r.y
	field: r.w
	field: r.h
	field: r.z
	field: r.lit
end-structure
: rooms room * ;

0 constant basement
1 constant attic
2 constant third-floor
3 constant second-floor
4 constant ground-floor
1 constant basement-rooms
1 constant attic-rooms
2 constant third-floor-rooms
3 constant second-floor-rooms
5 constant ground-floor-rooms

here 255 cells allot constant room-array
22 constant height
80 constant width
width height * constant map-size
here room allot constant map-frame
0 map-frame r.x !
0 map-frame r.y !
height map-frame r.h !
width map-frame r.w !

9608 constant echar-full-block

: dot ( n n  -- ) at-xy [char] . emit ;
: vdot ( n n -- ) at-xy [char] x emit ;
: hdot ( n n -- ) at-xy [char] x emit ;
: vertical-line ( n n n -- ) 0 do 2dup i + vdot loop 2drop ;
: horizontal-line ( n n n -- ) 0 do 2dup i rot + swap hdot loop 2drop ;

: box ( x y width height -- )
	{ x y width height }
	x width + y height vertical-line
	x y height vertical-line 
	x y width horizontal-line
	x y height + width horizontal-line ;

: .print-echar ( echar x y -- ) 
	at-xy xemit ;
: .room { room -- } 
	room r.x @
	room r.y @
	room r.w @
	room r.h @ box ;
: random-room ( -- room )
	here room allot >r
	3d6 r@ r.w !
	0 width r@ r.w @ - random-in-range r@ r.x !		
	3d6 r@ r.h !
	0 height r@ r.h @ - random-in-range r@ r.y !		
	r> ;

page

