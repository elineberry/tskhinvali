: dot ( n n  -- ) at-xy [char] . emit ;
: vdot ( n n -- ) at-xy [char] | emit ;
: hdot ( n n -- ) at-xy [char] - emit ;
: vertical-line ( n n n -- ) 0 do 2dup i + vdot loop 2drop ;
: horizontal-line ( n n n -- ) 0 do 2dup i rot + swap hdot loop 2drop ;

: box ( x y width height -- )
	{ x y width height }
	x width + y height vertical-line
	x y height vertical-line 
	x y width horizontal-line
	x y height + width horizontal-line ;

: box-top ( x y width height -- ) drop horizontal-line ;
: box-bottom ( x y width height -- ) ;

: test-box page 3 5 8 11 box ;
