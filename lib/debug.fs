\ ### DEBUG HELPERS ###
false constant DEBUG
true constant POPULATE?
: BREAKPT DEBUG if 0 28 at-xy .s key drop then ;
: BRK BREAKPT ;
: .debug-check-end cr cr depth 0 <> 
	if ." ### YOU'VE GOT A PROBLEM ###" cr .s cr 
	else ." √" 
	then cr cr ;
