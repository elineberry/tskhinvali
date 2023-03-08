defer 'map! ( x y char -- )

9612 constant l-wall-glyph 		\ Hex 258C ▌ 
9616 constant r-wall-glyph		\ Hex 2590 ▐
9620 constant u-wall-glyph		\ HEx 2594 ▔
9601 constant b-wall-glyph		\ hex 2581 ▁
1 constant v-glyph
1 constant h-glyph

: dot ( n n  -- ) [char] . 'map! ;
: l-wall ( n n -- ) at-xy l-wall-glyph xemit ;
: vdot ( n n -- ) at-xy v-glyph xemit ;
: hdot ( n n -- ) at-xy h-glyph xemit ;
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
