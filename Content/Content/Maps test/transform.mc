﻿Keyword = "$" LeftArguments = [] RightArguments = [<<System.Collections.Immutable.ImmutableDictionary<int, string>>>] Priority = 10000 Class = "MapIntString"
Keyword = "run" LeftArguments = [] RightArguments = [MapIntString] Priority = 0 Class = "Expr"
Keyword = "add" LeftArguments = [MapIntString] RightArguments = [<<int>> <<string>>] Priority = 1 Class = "Expr"


M' := <<M.Add(k,v)>>
---------------------
$M add k v => $M'

M add 1 "one" => M1
M1 add 2 "two" => M2
M2 add 3 "three" => M3
-----------------------
run M => M3