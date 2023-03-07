require ./lib/random-util.fs
require ./lib/util.fs

\ prompt until they have been asked three questions
\ or have typed a certain number of characters

5 value intro.input.x
10 value intro.input.y
80 value intro.width
24 value intro.height

: int:centered-x ( u -- x ) intro.width swap - 2 / ;
: .int:centered ( addr u -- ) dup int:centered-x 5 at-xy type ;
: .int:formatted-title ( addr u -- )
	.int:centered 
;
: .int:press-key-prompt s" -- press any key to continue --"
	dup int:centered-x intro.height at-xy type key drop ;


: .input-field ( -- n ) 
	intro.input.x intro.input.y at-xy
	75 intro.input.x do bl emit loop 
	intro.input.x intro.input.y at-xy
	pad 70 accept ;
: .intro.prompt ( addr n -- ) 
	.int:centered ;
: .intro ( -- n )
	page
	s" you have died..." .intro.prompt
	1000 ms
	s" Tell me about your life..." .intro.prompt .input-field
	s" ...what did you love.." .intro.prompt .input-field
	s" .what brought you joy..." .intro.prompt .input-field
;
