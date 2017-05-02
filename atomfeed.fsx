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
        Date: string
        Posts: FeedEntry []
    }

let private generateFeedEntries entries =
    entries
    |> Array.map (fun entry -> 
        XElementNS "entry" "http://www.w3.org/2005/Atom" [
            XElementNS "title" "http://www.w3.org/2005/Atom" [ string entry.Title ]
            XElementNS "link" "http://www.w3.org/2005/Atom" [
                XAttribute "href" "http://bittacklr.be/feed.atom"
            ]
            XElementNS "updated" "http://www.w3.org/2005/Atom" [ string entry.Date ]
            XElementNS "id" "http://www.w3.org/2005/Atom" [ string (sprintf "urn:bittacklr:%s" entry.Id) ]
            XElementNS "content" "http://www.w3.org/2005/Atom" [
                XAttribute "type" "html"
                box entry.Content
            ]
        ]
    )

/// Functions
let private generateFeed feed =
    XDocument (XDeclaration "1.0" "UTF-8" "yes") [
        XElementNSSeq "feed" "http://www.w3.org/2005/Atom" 
            [
                [|
                    XElementNS "title" "http://www.w3.org/2005/Atom" [ string "The BitTacklr Blog"]
                    XElementNS "link" "http://www.w3.org/2005/Atom" [
                        XAttribute "href" "http://bittacklr.be/feed.atom"
                        XAttribute "rel" "self"
                    ]
                    XElementNS "link" "http://www.w3.org/2005/Atom" [
                        XAttribute "href" "http://bittacklr.be/"
                    ]
                    XElementNS "updated" "http://www.w3.org/2005/Atom" [ string feed.Date ]
                    XElementNS "id" "http://www.w3.org/2005/Atom" [ string "http://bittacklr.be/" ]
                    XElementNS "author" "http://www.w3.org/2005/Atom" [
                        XElementNS "name" "http://www.w3.org/2005/Atom" [ string "Yves Reynhout" ]
                        XElementNS "email" "http://www.w3.org/2005/Atom" [ string "yves.reynhout@bittacklr.be" ]
                    ]
                |]
                generateFeedEntries feed.Posts  
            ]
        ]

let generate (feed: Feed) (siteDir: DirectoryInfo) =
    let document = generateFeed feed
    let feedFile = FileInfo(Path.Combine(siteDir.FullName, "bittacklr-atom.xml"))
    document.Save feedFile.FullName