using System;
using System.Collections.Generic;
using System.Linq;
namespace stsil {
 public static class Extensions { public static V GetKey<T, V>(this System.Collections.Immutable.ImmutableDictionary<T, V> self, T key) { return self[key]; } }



public interface ListInt {}
public interface Expr {}



public class dda : Expr  {
public ListInt P1;

public dda(ListInt P1) {this.P1 = P1;}
public static dda Create(ListInt P1) { return new dda(P1); }

  public static int StaticRun(ListInt P1) {    
 { 
 var tmp_0 = P1 as lin; 
if (tmp_0 != null) { 
 
var result = 0;
 return result;  }
 } 

  
 { 
 var tmp_0 = P1 as snoc; 
if (tmp_0 != null) { 
 var xs = tmp_0.P1; var x = tmp_0.P2; 
var tmp_2 = dda.Create(xs);

var tmp_1 = tmp_2.Run();

var res = tmp_1; 
var result = (x+res);
 return result;  }
 } 

  
throw new System.Exception("Error evaluating: " + new dda(P1).ToString() + " no result returned."); }
public int Run() { return StaticRun(P1); }


public override string ToString() {
 var res = "("; 

 res += " dda "; if (P1 is System.Collections.IEnumerable) { res += "{"; foreach(var x in P1 as System.Collections.IEnumerable) res += x.ToString(); res += "}";  } else { res += P1.ToString(); } 

 res += ")";
 return res;
}

public override bool Equals(object other) {
 var tmp = other as dda;
 if(tmp != null) return this.P1.Equals(tmp.P1); 
 else return false; }

public override int GetHashCode() {
 return 0; 
}

}

public class lin : ListInt  {

public lin() {}
public static lin Create() { return new lin(); }

public override string ToString() {
return "lin";
}

public override bool Equals(object other) {
 return other is lin; 
}

public override int GetHashCode() {
 return 0; 
}

}

public class snoc : ListInt  {
public ListInt P1;
public int P2;

public snoc(ListInt P1, int P2) {this.P1 = P1; this.P2 = P2;}
public static snoc Create(ListInt P1, int P2) { return new snoc(P1, P2); }

public override string ToString() {
 var res = "("; 
if (P1 is System.Collections.IEnumerable) { res += "{"; foreach(var x in P1 as System.Collections.IEnumerable) res += x.ToString(); res += "}";  } else { res += P1.ToString(); } 

 res += " snoc "; res += P2.ToString(); 

 res += ")";
 return res;
}

public override bool Equals(object other) {
 var tmp = other as snoc;
 if(tmp != null) return this.P1.Equals(tmp.P1) && this.P2.Equals(tmp.P2); 
 else return false; }

public override int GetHashCode() {
 return 0; 
}

}




public class EntryPoint {
static public object Run(bool printInput)
{
 #line 1 "input"
 var p = dda.Create(snoc.Create(snoc.Create(snoc.Create(lin.Create(), 3), 2), 1));
if(printInput) System.Console.WriteLine(p.ToString());
 
 var result = p.Run(); 

return result;
}
}

}
