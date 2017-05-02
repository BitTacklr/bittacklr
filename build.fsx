#I "packages/FAKE/tools"
#r "FakeLib.dll" // include Fake lib
#load "blog.fsx"

open Fake

open System
open System.Diagnostics
open System.IO

let siteDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "site"))
let srcDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "src"))
let templatesDir = DirectoryInfo(Path.Combine(srcDir.FullName, "templates"))
let postsDir = DirectoryInfo(Path.Combine(srcDir.FullName, "posts"))

Target "Clean" (fun () -> DeleteDir siteDir.FullName)

Target "CopyStaticContent" (fun () -> 
    !! "src/*.html"
    ++ "src/*.css"
    ++ "src/*.js"
    ++ "src/images/*.svg"
    ++ "src/fonts/*.eot"
    ++ "src/fonts/*.svg"
    ++ "src/fonts/*.ttf"
    ++ "src/fonts/*.woff"
    ++ "src/fonts/*.woff2"
    |> Seq.iter (fun path -> 
        CopyFileWithSubfolder srcDir.FullName siteDir.FullName path
    )
)

Target "Init" (fun () -> 
    CreateDir siteDir.FullName
)

Target "Build" (fun () -> 
    Blog.generate postsDir templatesDir siteDir
)

"Clean"
==> "Init"
==> "CopyStaticContent"
==> "Build"

RunTargetOrDefault "Build"