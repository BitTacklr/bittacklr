#r "System.Xml.Linq"

open System.Text
open System.Xml
open System.Xml.Linq
open System.IO
 
// XDocument Syntax
let private XDeclaration version encoding standalone = XDeclaration(version, encoding, standalone)
let private XLocalName localName namespaceName = XName.Get(localName, namespaceName)
let private XName expandedName = XName.Get(expandedName)
let private XDocument xdecl content = XDocument(xdecl, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray)
let private XComment (value:string) = XComment(value) :> obj
let private XElementNS localName namespaceName content = XElement(XLocalName localName namespaceName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj
let private XElementNSSeq localName namespaceName sources = XElementNS localName namespaceName (Seq.concat sources)
let private XElement expandedName content = XElement(XName expandedName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj
let private XElementSeq expandedName sources = XElement expandedName (Seq.concat sources)
let private XAttributeNS localName namespaceName value = XAttribute(XLocalName localName namespaceName, value) :> obj
let private XAttribute expandedName value = XAttribute(XName expandedName, value) :> obj

/// Model
type FeedEntry =
    {
        Id: string
        Title: string
        RelativeUrl: string
        Date: string
        Content: string
    }
type Feed = 
    {
        SiteUrl: string
        BaseUrl: string 
        Date: string
        Entries: FeedEntry []
    }

let private ns = "http://www.w3.org/2005/Atom"
let private generateFeedEntries feed =
    feed.Entries
    |> Array.map (fun entry -> 
        XElementNS "entry" ns [
            XElementNS "title" ns [ string entry.Title ]
            XElementNS "link" ns [
                XAttribute "href" (sprintf "%s/%s" feed.BaseUrl entry.RelativeUrl)
            ]
            XElementNS "updated" ns [ string entry.Date ]
            XElementNS "id" ns [ string (sprintf "urn:bittacklr:%s" entry.Id) ]
            XElementNS "content" ns [
                XAttribute "type" "html"
                box entry.Content
            ]
        ]
    )

/// Functions
let private generateFeed feed =
    XDocument (XDeclaration "1.0" "UTF-8" "yes") [
        XElementNSSeq "feed" ns 
            [
                [|
                    XElementNS "title" ns [ string "The BitTacklr Blog"]
                    XElementNS "link" ns [
                        XAttribute "href" (sprintf "%s/bittacklr.atom" feed.SiteUrl)
                        XAttribute "rel" "self"
                    ]
                    XElementNS "link" ns [
                        XAttribute "href" feed.SiteUrl
                    ]
                    XElementNS "updated" ns [ string feed.Date ]
                    XElementNS "id" ns [ string feed.BaseUrl ]
                    XElementNS "author" ns [
                        XElementNS "name" ns [ string "Yves Reynhout" ]
                        XElementNS "email" ns [ string "yves.reynhout@bittacklr.be" ]
                    ]
                |]
                generateFeedEntries feed
            ]
        ]

let generate (feed: Feed) (siteDir: DirectoryInfo) =
    let document = generateFeed feed
    let feedFile = FileInfo(Path.Combine(siteDir.FullName, "bittacklr.atom"))
    document.Save feedFile.FullName