﻿import UnityEngine

Func "getPosition" -> << WrapperTest >> : Getter => << Vector3 >>
Func << WrapperTest >> -> "setPosition" -> << Vector3 >> : Setter => Unit
Func "testImported" : Test => << Vector3 >>
Data "unit" : Unit

<< x.Position >> => res
-----------------------------
getPosition x => res

<< x.Position = v >> => u
-----------------------
x setPosition v => unit

<< new Vector3(0.0,1.0,0.0) >> => v1
<< new Vector3(0.0,2.0,1.0) >> => v2
<< WrapperTest.Instantiate(v1) >> => x
<< v1 + v2 >> => v
x setPosition v => u
getPosition x => res
-----------------------------
testImported => res


