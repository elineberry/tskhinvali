: allot-erase ( n -- addr )	\ size in chars
    here over allot swap over swap erase ;
: escape-code ESC[ 0 .r ." m" ;
: hide-cursor ESC[ ." ?25l" ;
: show-cursor ESC[ ." ?25h" ;
: tty-reset 0 escape-code ;
: tty-bold	1 escape-code ;
: tty-dim		2 escape-code ;
: tty-inverse 7 escape-code ;
: reverse-text tty-inverse ;
: regular-text tty-reset tty-dim ;
: fov-text tty-reset tty-bold ;
: bold-text tty-reset tty-inverse tty-bold ;
: util:set-bg 40 escape-code ;
: util:set-fg 38 escape-code ;
: tree-color 33 escape-code ; \ 43 escape-code ;
: shrub-color 34 escape-code ;
: util:set-colors
	util:set-bg
	util:set-fg ;
: reset-colors tty-reset util:set-colors ;
: toast { addr n -- key } 
    bold-text
    80 n - 2 / 25 5 - 2 / 
    3 0 do 
    2dup i + at-xy n 6 + 0 do BL emit loop loop
    swap 3 + swap 1+ at-xy addr n type
    tty-reset
    key ;
4 constant tab-stop
: .space bl emit ;
: .tab tab-stop 0 do bl emit loop ;
: util:out-of-memory abort" Probably out of memory" ;
: allocate-or-abort allocate util:out-of-memory ;

: sq ( n -- n ) dup * ;
: sqrt ( n -- n ) s>f fsqrt f>s ;

