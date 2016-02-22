﻿module ExtractFromToken2

open ParserMonad
open Lexer2
open Common

let extract_position_from_token (token:Token) :Position =
  match token with  
  | Lexer2.Id(_,pos) -> pos
  | Lexer2.VarId(_,pos) -> pos
  | Lexer2.Keyword(_,pos) -> pos
  | Lexer2.Literal(_,pos) -> pos

let extract_keyword() :Parser<Token,_,Keyword*Position> =
  prs{
    let! next = step
    match next with
    | Keyword(k,p) -> return k,p
    | _ -> return! fail (MatchError ("Keyword",extract_position_from_token next))
  }

let extract_id() :Parser<Token,_,Id*Position> =
  prs{
    let! next = step
    match next with
    | Lexer2.Id(i,p) -> return i,p
    | _ -> return! fail (MatchError ("Id",extract_position_from_token next))
  }

let extract_varid() :Parser<Token,_,Id*Position> =
  prs{
    let! next = step
    match next with
    | Lexer2.VarId(i,p) -> return i,p
    | _ -> return! fail (MatchError ("VarId",extract_position_from_token next))
  }

let extract_string_literal() :Parser<Token,_,string*Position> =
  prs{
    let! next = step
    match next with
    | Lexer2.Literal(String(str),pos) -> return str,pos
    | _ -> return! fail (MatchError ("string literal",extract_position_from_token next))
  }

let extract_int_literal() :Parser<Token,_,int*Position> =
  prs{
    let! next = step
    match next with
    | Lexer2.Literal(Int(i),pos) -> return i,pos
    | _ -> return! fail (MatchError ("int literal",extract_position_from_token next))
  }

let check_keyword() (expected:Keyword) :Parser<Token,_,_> =
  prs{
    let! k,p = extract_keyword()
    if k = expected then return () else return! fail (ParserError p)
  }
