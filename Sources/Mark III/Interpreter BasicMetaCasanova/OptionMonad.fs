﻿module OptionMonad

open ParserMonad

type OptionBuilder() =
  member this.Return (res:'a) : Option<'a> = Some(res)
  member this.Bind (p:Option<'a>,k:'a->Option<'b>):Option<'b> = Option.bind k p
  member this.ReturnFrom p = p
  
let opt = OptionBuilder()

let option_to_bool (op:Option<'a>) :bool =
  match op with
  | Some(_) -> true
  | None    -> false

let unpack_option (op:Option<'a>): 'a =
  match op with
  | Some a -> a
  | None   -> failwith "one of the options failed to unpack."

let try_unpack_list_of_option (oplist:List<Option<'a>>) :Option<List<'a>> =
  opt{
    if List.forall option_to_bool oplist then
      return List.collect (fun x -> [unpack_option x]) oplist
    else return! None
  }

let use_parser_monad (pars:Parser<'char,'ctxt,'res>)(input:List<'char>*'ctxt) :Option<'res> =
  let convert = 
    match pars (input) with
    | Done(res,_,_) -> Some(res)
    | Error e ->
      printfn "%A" e
      None 
  opt{return! convert}

let option_to_list (op:Option<'a>) :List<'a> = if op.IsSome then [op.Value] else []