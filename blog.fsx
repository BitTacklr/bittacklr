#I "packages/FAKE/tools"
#r "FakeLib.dll" // include Fake lib
#r "packages/YamlDotNet/lib/net35/YamlDotNet.dll"
#load "atomfeed.fsx"

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Globalization
open System.Threading
open System.Security

open YamlDotNet.RepresentationModel
open Atomfeed

open Fake
/// Settings
let private timeToRenderBlogPost = Convert.ToInt32(TimeSpan.FromSeconds(5.0).TotalMilliseconds)
let private blogPostAssetPatterns = 
    [| 
        "*.svg"
        "*.png"
        "*.jpg" 
    |]
let private englishCulture = CultureInfo("en-US")

/// Model
type private BlogPostMetadata =
    {
        Title: string option
        Date: DateTime option
        Author: string
        Published: bool
        Tags: string []
    }
    with static member empty = { Title = None; Date = None; Author = "Yves Reynhout"; Published = false; Tags = Array.empty }

type private BlogPost =
    {
        BaseDirectory: DirectoryInfo
        File: FileInfo
        Metadata: BlogPostMetadata
    }
    member this.Date 
        with get() = 
            match this.Metadata.Date with
            | Some date -> date
            | None -> this.File.CreationTimeUtc

    member this.Id 
        with get() = 
            this.File.FullName.Substring(this.BaseDirectory.FullName.Length + 1).Replace(Path.DirectorySeparatorChar, '-').Replace(Path.AltDirectorySeparatorChar, '-')

    member this.ResolveTargetFile (targetDir: DirectoryInfo) =
        let filename =
            match this.Metadata.Title with
            | Some title -> sprintf "%s.html" title
            | None -> Path.ChangeExtension(this.File.Name, "html")
        let relativePath =
            match this.Metadata.Date with
            | Some date ->
                sprintf "%04d/%s/%02d" date.Year (englishCulture.DateTimeFormat.GetMonthName(date.Month).ToLowerInvariant()) date.Day
            | None ->
                this.File.DirectoryName.Substring(this.BaseDirectory.FullName.Length + 1)
        FileInfo(Path.Combine(targetDir.FullName, Path.Combine(relativePath, filename)))

type private BlogPostMetadataVisitor() =
    inherit YamlVisitorBase()
    let mutable metadata = BlogPostMetadata.empty
    override this.VisitPair (key: YamlNode, value: YamlNode) =
        match key with
        | :? YamlScalarNode as k ->
            match k.Value with
            | "title" -> 
                match value with
                | :? YamlScalarNode as v ->
                    metadata <- { metadata with Title = Some(v.Value) }
                | _ -> ()
            | "date" -> 
                match value with
                | :? YamlScalarNode as v ->
                    let (parsed, date) = DateTimeOffset.TryParse(v.Value)
                    if parsed then metadata <- { metadata with Date = Some(date.Date) }
                | _ -> ()
            | "author" ->
                match value with
                | :? YamlScalarNode as v ->
                    metadata <- { metadata with Author = v.Value }
                | _ -> ()
            | "publish" ->
                match value with
                | :? YamlScalarNode as v ->
                    metadata <- { metadata with Published = Boolean.Parse(v.Value) }
                | _ -> ()
            | "tags" ->
                match value with
                | :? YamlSequenceNode as v ->
                    let tags =
                        v.Children 
                        |> Seq.choose (fun child -> 
                            match child with
                            | :? YamlScalarNode as cv -> Some(cv)
                            | _ -> None
                        )
                        |> Seq.map (fun scalar -> scalar.Value)
                        |> Seq.toArray
                    
                    metadata <- { metadata with Tags = tags }
                | _ -> ()
            | _ -> ()
        | _ -> ()
        base.VisitPair(key, value) 
    member this.Metadata with get() = metadata

/// Functions
let private readBlogPostMetadata (file: FileInfo) =
    let lines = File.ReadAllLines(file.FullName)
    let frontmatter = lines |> Array.skip 1 |> Array.takeWhile (fun line -> not(line.StartsWith("---") || line.StartsWith("..."))) |> String.concat Environment.NewLine
    using(new StringReader(frontmatter)) (fun reader ->
        let stream = YamlStream()
        stream.Load(reader)
        match stream.Documents.Count with
        | 1 -> 
            let visitor = BlogPostMetadataVisitor()
            stream.Accept(visitor)
            visitor.Metadata
        | _ -> 
            BlogPostMetadata.empty
    )

let private renderBlogPost (template: FileInfo) (input: FileInfo) (output: FileInfo) =
    let info = ProcessStartInfo("pandoc", sprintf "--read=markdown_github+yaml_metadata_block --write=html --template='%s' --output='%s' '%s'" template.FullName output.FullName input.FullName)
    use pandoc = Process.Start(info)
    if not(pandoc.WaitForExit(timeToRenderBlogPost)) then failwith (sprintf "Rendering %s using pandoc took longer than %dms." input.FullName timeToRenderBlogPost)
    if pandoc.ExitCode <> 0 then failwith (sprintf "Rendering %s using pandoc failed with exit code %d." input.FullName pandoc.ExitCode)

let private generateBlogPosts (blogPosts: BlogPost []) (blogPostTemplate: FileInfo) (siteDir: DirectoryInfo) =
    blogPosts
    |> Array.iter (fun blogPost -> 
        let targetFile = blogPost.ResolveTargetFile siteDir
        if not(targetFile.Directory.Exists) then targetFile.Directory.Create()

        renderBlogPost blogPostTemplate blogPost.File targetFile

        blogPostAssetPatterns
        |> Array.iter (fun blogPostAssetPattern ->
            blogPost.File.Directory.GetFiles(blogPostAssetPattern, SearchOption.AllDirectories)
            |> Seq.iter (fun blogPostAssetFile -> 
                File.Copy(blogPostAssetFile.FullName, Path.Combine(targetFile.Directory.FullName, blogPostAssetFile.Name))
            )
        )
    )


let private generateAtomFeed (blogPosts: BlogPost []) (atomFeedTemplate: FileInfo) (siteDir: DirectoryInfo) =
    let latest = blogPosts |> Array.map (fun blogPost -> blogPost.Date) |> Array.max
    let feed = 
        { 
            Date = sprintf "%sT00:00:00Z" (latest.ToString("yyyy-MM-dd"))
            Posts = 
                blogPosts 
                |> Array.map (fun blogPost ->
                    let targetFile = blogPost.ResolveTargetFile siteDir
                    {
                        Id = blogPost.Id
                        Title = Option.defaultValue (Path.GetFileNameWithoutExtension(blogPost.File.Name)) blogPost.Metadata.Title
                        RelativeUrl = targetFile.FullName.Substring(siteDir.FullName.Length + 1)
                        Date = sprintf "%sT00:00:00Z" (blogPost.Date.ToString("yyyy-MM-dd"))
                        Content = SecurityElement.Escape(File.ReadAllText(targetFile.FullName))
                    }
                )
        }
    Atomfeed.generate feed siteDir

let generate (blogPostsDir: DirectoryInfo) (templatesDir: DirectoryInfo) (siteDir: DirectoryInfo) =
    // Generate blogposts
    let allBlogPostFiles = blogPostsDir.GetFiles("*.md", SearchOption.AllDirectories)

    let blogPosts =
        allBlogPostFiles
        |> Array.map (fun (file: FileInfo) -> 
            { 
                BaseDirectory = blogPostsDir
                File = file
                Metadata = readBlogPostMetadata file
            }
        )
        |> Array.filter (fun blogPost -> blogPost.Metadata.Published)

    let blogPostTemplate = FileInfo(Path.Combine(templatesDir.FullName, "blogpost.html"))
    generateBlogPosts blogPosts blogPostTemplate siteDir
    
    let atomFeedTemplate = FileInfo(Path.Combine(templatesDir.FullName, "bittacklr.atom"))
    generateAtomFeed blogPosts atomFeedTemplate siteDir

