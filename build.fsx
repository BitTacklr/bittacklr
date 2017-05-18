#I "packages/FAKE/tools"
#r "FakeLib.dll" // include Fake lib
#load "blog.fsx"

open Fake

open System
open System.Diagnostics
open System.IO

open AngleSharp
open AngleSharp.Dom
open AngleSharp.Dom.Html
open AngleSharp.Extensions
open AngleSharp.Html
open AngleSharp.Parser.Html

//dev
// let siteUrl = "http://127.0.0.1:8000/site"
// //dev online
// let siteUrl = "http://bittacklr.bitballoon.com"
//production
let siteUrl = "http://bittacklr.be"

let siteDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "site"))
let siteBlogDir = DirectoryInfo(Path.Combine(siteDir.FullName, "blog"))
let srcDir = DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "src"))
let templatesDir = DirectoryInfo(Path.Combine(srcDir.FullName, "templates"))
let blogPostsDir = DirectoryInfo(Path.Combine(srcDir.FullName, "posts"))

/// Settings
let private timeToCompileLess = TimeSpan.FromSeconds(5.0)
let private dotless = Path.Combine(Path.Combine(Path.Combine(Path.Combine(__SOURCE_DIRECTORY__, "packages"), "dotless"), "tool"), "dotless.compiler.exe")

Target "Clean" (fun () -> DeleteDir siteDir.FullName)

let private compileLess (workingDir: DirectoryInfo) (stylesheet: string) =
    let result = 
        ExecProcess (fun info ->
                        if isLinux then
                            info.FileName <- dotless
                        elif isWindows then
                            info.FileName <- dotless
                        else
                            failwith "Only linux and windows are supported at this moment in time."
                        info.Arguments <- stylesheet
                        info.WorkingDirectory <- workingDir.FullName
                    ) timeToCompileLess
    if result <> 0 then
        failwith "Could not compile less in a timely fashion."

Target "CopyStaticContent" (fun () -> 
    !! "src/*.html"
    ++ "src/*.less"
    ++ "src/*.js"
    ++ "src/*.vcf"
    ++ "src/images/*.svg"
    ++ "src/fonts/*.eot"
    ++ "src/fonts/*.svg"
    ++ "src/fonts/*.ttf"
    ++ "src/fonts/*.woff"
    ++ "src/fonts/*.woff2"
    |> Seq.iter (fun path -> 
        CopyFileWithSubfolder srcDir.FullName siteDir.FullName path
    )
    
    compileLess siteDir "home"
    compileLess siteDir "services"
    compileLess siteDir "contact"
    compileLess siteDir "blog"
    
    !! "site/*.less" |> Seq.iter DeleteFile
)

Target "Init" (fun () -> 
    CreateDir siteDir.FullName
)

Target "Build" (fun () -> 
    //Blog
    let settings : Blog.BlogSettings =
        {
            SiteUrl = siteUrl
            SiteDir = siteDir
            SiteBlogDir = siteBlogDir
            BlogBaseUrl = sprintf "%s/blog" siteUrl
            SrcDir = srcDir
            BlogPostsDir = blogPostsDir
            TemplatesDir = templatesDir
        }
    Blog.generate settings
)

"Clean"
==> "Init"
==> "CopyStaticContent"
==> "Build"

RunTargetOrDefault "Build"