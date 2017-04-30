#r "packages/HtmlAgilityPack/lib/Net40/HtmlAgilityPack.dll"
#r "packages/Fue/lib/net45/Fue.dll"
open System
open Fue.Data
open Fue.Compiler

let compiled = init |> add "name" "Roman" |> fromText "{{{name}}}"