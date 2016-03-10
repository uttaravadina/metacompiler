﻿module Codegen
open Common
open ParserMonad
open CodegenInterface

// mangling ////////////////////////////////////////////////////////////////////
    
let genericMangle (name:string) :string =
  let readables = 
    Map.ofArray <| Array.zip "!#$%&'*+,-./\\:;<>=?@^_`|~"B [|
      "bang";"hash";"cash";"perc";"amp";"prime";"star";"plus";"comma";
      "dash"; "dot";"slash";"back";"colon";"semi";"less";"great";
      "equal";"quest";"at";"caret";"under";"tick";"pipe";"tilde"|]
  let mangleChar c =
    if (c>='A'&&c<='Z') || (c>='a'&&c<='z') || (c>='0'&&c<='9') then
      sprintf "%c" c
    else 
      let lookup = readables |> Map.tryFind (System.Convert.ToByte c)
      match lookup with
      | None   ->  failwith <| sprintf "ERROR(codegen): expecting printable ASCII character, got (0x%04X)" (System.Convert.ToUInt16(c))
      | Some x -> sprintf "_%s" x
  name |> String.collect mangleChar

let CSharpMangle (name:string) :string =
  let keywords = Set.ofArray [| "abstract" ; "as" ; "base" ; "bool" ; 
    "break" ; "byte" ; "case" ; "catch" ; "char" ; "checked" ; "class" ; "const" ;
    "continue" ; "decimal" ; "default" ; "delegate" ; "do" ; "double" ; "else" ;
    "enum" ; "event" ; "explicit" ; "extern" ; "false" ; "finally" ; "fixed" ; 
    "float" ; "for" ; "foreach" ; "goto" ; "if" ; "implicit" ; "in" ; "int" ; 
    "interface" ; "internal" ; "is" ; "lock" ; "long" ; "namespace" ; "new" ;
    "null" ; "object" ; "operator" ; "out" ; "override" ; "params" ; "private" ;
    "protected" ; "public" ; "readonly" ; "ref" ; "return" ; "sbyte" ; "sealed" ;
    "short" ; "sizeof" ; "stackalloc" ; "static" ; "string" ; "struct" ; 
    "switch" ; "this" ; "throw" ; "true" ; "try" ; "typeof" ; "uint" ; "ulong" ; 
    "unchecked" ; "unsafe" ; "ushort" ; "using" ; "virtual" ; "void" ; 
    "volatile" ; "while" |]
  let name = genericMangle name
  if keywords.Contains(name) then sprintf "@%s" name else name 

let rec mangle_type_suffix(t:Type):string=
  match t with
  | DotNetType (id) -> id.Namespace@[id.Name] |> Seq.map genericMangle |> String.concat "_ns"
  | McType     (id) -> id.Namespace@[id.Name] |> Seq.map genericMangle |> String.concat "_ns"
  | TypeApplication (fn,lst) -> fn::lst |> List.map mangle_type_suffix |> String.concat "_of"
  
let rec mangle_type(t:Type):string=
  match t with
  | DotNetType (id) -> id.Namespace@[id.Name] |> Seq.map CSharpMangle |> String.concat "."
  | McType     (id) -> id.Namespace@[id.Name] |> Seq.map (fun x->if x="System" then "_System" else CSharpMangle x) |> String.concat "."
  | TypeApplication (fn,lst) -> mangle_type fn


let print_local_id n = match n with Named x -> CSharpMangle x | Tmp x -> sprintf "_tmp%d" x 

type NamespacedItem = Ns       of string*List<NamespacedItem>
                    | Data     of string*data
                    | Function of string*List<rule>
                    | Lambda   of int*rule

let construct_tree (input:fromTypecheckerWithLove) :List<NamespacedItem> =
  let rec datatree (s:List<NamespacedItem>) (idx:List<string>,v:string*data) :List<NamespacedItem> =
    match idx with
    | []    -> (Data(v))::s
    | n::ns -> match s |> List.partition (fun x->match x with Ns(n,_)->true | _->false) with
               | [Ns(n,body)],rest -> Ns(n,datatree body (ns,v))::rest
               | [],list           -> Ns(n,datatree []   (ns,v))::list
  let rec functree (s:List<NamespacedItem>) (idx:List<string>,v:string*List<rule>) :List<NamespacedItem> =
    match idx with
    | []    -> (Function(v))::s
    | n::ns -> match s |> List.partition (fun x->match x with Ns(n,_)->true | _->false) with
               | [Ns(n,body)],rest -> Ns(n,functree body (ns,v))::rest
               | [],list           -> Ns(n,functree []   (ns,v))::list
  let rec lambdatree (s:List<NamespacedItem>) (idx:List<string>,v:int*rule) :List<NamespacedItem> =
    match idx with
    | []    -> (Lambda(v))::s
    | n::ns -> match s |> List.partition (fun x->match x with Ns(n,_)->true | _->false) with
               | [Ns(n,body)],rest -> Ns(n,lambdatree body (ns,v))::rest
               | [],list           -> Ns(n,lambdatree []   (ns,v))::list
  let go input output state = input |> Seq.map (fun (n,d)->n.Namespace,(n.Name,d)) |> Seq.fold output state
  [] |> go (Map.toSeq input.lambdas) lambdatree 
     |> go (Map.toSeq input.rules) functree 
     |> go input.datas datatree

let print_literal lit =
  match lit with
  | I64 i    -> sprintf "%d" i
  | U64 i    -> sprintf "%u" i
  | F64 i    -> sprintf "%f" i
  | String s -> sprintf "\"%s\"" s
  | Bool b   -> if b then "true" else "false"

let field (n:local_id,t:Type) = sprintf "public %s %s;\n" (mangle_type t) (print_local_id n)

let highest_tmp (typemap:Map<local_id,Type>): int =
  typemap |> Map.fold (fun s k _ -> match k with Tmp(x) when x>s -> x | _ -> s) 0

let rec print_tree (lookup:fromTypecheckerWithLove) (ns:List<NamespacedItem>) :string =
  let build_func (name:string) (rules:rule list) = 
    let rule = List.head rules
    let args = "/*ARGS*/" //rule.input |> Seq.map (fun id-> rule.typemap.[id])
    let ret_type = "/*RET*/"
    let rules = "/*RULES*/"
    sprintf "class %s{\n%s\npublic List<%s> run(){\nList<%s> _ret;\n%s\nreturn _ret;\n}\n}\n" (CSharpMangle name) args ret_type ret_type rules
  let print_base_types (ns:List<NamespacedItem>) = 
    let types = ns |> List.fold (fun types item -> match item with Data (_,v) -> v.outputType::types | _ -> types) [] |> List.distinct
    let print t = sprintf "public class %s{}\n" (t|>mangle_type_suffix)
    List.map print types
  let go ns =
    match ns with
    | Ns(n,ns)      -> sprintf "namespace %s{\n%s}\n" (CSharpMangle n) (print_tree lookup ns)
    | Data(n,d)     -> sprintf "public class %s:%s{\n%s}\n" (CSharpMangle n) (d.outputType|>mangle_type_suffix) (d.args|>List.map field|>String.concat "")
    | Function(name,rules) -> build_func name rules
    | Lambda(number,rule)  -> build_func (sprintf "_L%d" number) [rule]
  (print_base_types ns)@(ns|>List.map go)|>String.concat "\n"
    
let failsafe_codegen(input:fromTypecheckerWithLove) =
  input |> construct_tree |> print_tree input |> printf "%s"

// TEST DATA ///////////////////////////////////////////////////////////////////

let test_data:fromTypecheckerWithLove=
  let int_t:Type   = DotNetType({Namespace=["System"];Name="Int32"})
  let float_t:Type = DotNetType({Namespace=["System"];Name="Single"})
  let star_t:Type  = TypeApplication((McType({Namespace=["mc";"test"];Name="star"})),[int_t;float_t])
  let pipe_t:Type  = TypeApplication((McType({Namespace=["mc";"test"];Name="pipe"})),[int_t;float_t])
  let comma_id:Id  = {Namespace=["mc";"test"];Name="comma"}
  let left_id:Id   = {Namespace=["mc";"test"];Name="left"}
  let right_id:Id  = {Namespace=["mc";"test"];Name="right"}
  let comma_data:data = 
    { 
      args=[Named("a"),int_t; Named("b"),float_t; ];
      outputType=star_t;
    }
  let left_data:data =
    {
      args=[Named("a"),int_t;]; 
      outputType=pipe_t;
    }
  let right_data:data =
    {
      args=[Named("a"),float_t;]; 
      outputType=pipe_t;
    }
  let datas = [comma_id,comma_data; left_id,left_data; right_id,right_data]
  let main = {input=[];output=Tmp(0);premis=[];typemap=Map.empty;side_effect=true}
  {rules=Map.empty;lambdas=Map.empty;datas=datas;main=main}

let list_test:fromTypecheckerWithLove =
  let int_t:Type   = DotNetType({Namespace=["System"];Name="Int32"})
  let add_t:Type = Arrow(int_t,Arrow(int_t,int_t))
  let add_id:Id  = {Namespace=["builtin"];Name="add"}

  // data "nil" -> List int
  let list_t:Type  = TypeApplication((McType({Namespace=["mc";"test"];Name="List"})),[int_t])
  let nil_id:Id    = {Namespace=["mc";"test"];Name="nil"}
  let nil_data:data =
    {
      args=[];
      outputType=list_t;
    }

  // data x -> "::" -> xs -> List int
  let append_id:Id = {Namespace=["mc";"test"];Name="::"}
  let append_data:data =
    {
      args=[Named("x"),int_t; Named("xs"),int_t];
      outputType=list_t;
    }
  
  // length nil -> 0
  let length_t:Type= Arrow (list_t,int_t)
  let length_id:Id = {Namespace=["mc";"test"];Name="length"}
  let length_nil:rule =
    {
      side_effect=false
      input=[Tmp(0)]
      premis=[Destructor({source=Tmp(0); destructor=nil_id; args=[]})
              Literal   ({value=I64(0L);dest=Tmp(1)}) ]
      output=Tmp(1)
      typemap=Map.ofSeq [Tmp(0),list_t
                         Tmp(1),int_t ]
    }

  // length xs -> r
  // -------------------------------
  // length x::xs -> add^builtin r 0
  let length_append:rule =
    {
      side_effect=false
      input=[Tmp(0)]
      premis=[Destructor({source=Tmp(0); destructor=append_id; args=[Named("x");Named("xs")]})
              McClosure({func=Func(length_id); dest=Tmp(1)})
              Application({closure=Tmp(1); argument=Named("xs"); dest=Named("r")})
              Literal({value=I64(0L); dest=Tmp(2)})
              DotNetClosure({func={Name="add";Namespace=["System";"Int32"]};dest=Tmp(3)})
              Application({closure=Tmp(3); argument=Named("r"); dest=Tmp(4)})
              Application({closure=Tmp(4); argument=Tmp(2);     dest=Tmp(5)}) ]
      output=Tmp(5)
      typemap=Map.ofSeq [Tmp(0),list_t
                         Named("x"),int_t
                         Named("xs"),list_t
                         Tmp(1),Arrow(list_t,int_t)
                         Named("r"),int_t
                         Tmp(2),int_t
                         Tmp(3),Arrow(int_t,(Arrow(int_t,int_t)))
                         Tmp(4),Arrow(int_t,int_t)
                         Tmp(5),int_t ]
    }

  let datas = [nil_id,nil_data; append_id,append_data]
  let Funcs = Map.ofSeq <| [length_id,[length_nil;length_append]]
  let main = {input=[];output=Tmp(0);premis=[];typemap=Map.empty;side_effect=true}
  {rules=Funcs;datas=datas;lambdas=Map.empty;main=main}