---
original: https://seabites.wordpress.com/2014/02/16/trenchtalk-visualdsls/
title: "Trench Talk: Visual DSLs - the need for communication"
slug: "trenchtalk-visualdsls"
date: 2014-02-16
author: Yves Reynhout
publish: true
---
As I'm slowly unwinding from a 5 year period of working on "something" in the area of electronic scheduling, I figured I'd share some of the experiences in modeling.

### A word about domain experts

Domain experts, you know, the people that work in a particular domain, come in all shapes and sizes. We may not want to admit it, but it is rare to find a **good** domain expert. Maybe you do interact with a domain expert directly. If so, good for you. Maybe a Business Analyst (BA) is the closest you as a developer get to a domain expert. Does that mean we can't practice domain driven design in that situation? Perhaps ... to me this is mainly about interaction with people and how well that BA plays the role of "enabler" for us developers to get an accurate "enough" understanding of the domain, instead of "proxy with loss in translation". The worst kind of domain expert proxy is probably a former techie. They have a tendency to bring the technical stuff into the conversation and don't mind telling you how to do your job. You can imagine how much that rubs me the wrong way. But often that's just the situation you are in. Are we still able to practice domain driven design at that point? I'm inclined to say no. It's a Don Quichotte fight ... deeper insight and breakthroughs are but a distant dream. That doesn't mean there is zero value to be reaped in that situation, but still, don't go shouting you're doing domain driven design. There are circumstances in which a domain expert is somebody very knowledgable about the domain but not necessarily working in the domain. Especially if you're building a product for a large number of customers, this person is probably in a better position than each one of those customers individually to see the bigger picture that aligns with the product "vision". Simply put, they have most, if not all, the angles on why a feature is required. This brings me to the most important question you need to ask domain experts: "**[Why?](http://en.wikipedia.org/wiki/5_Whys "The 5 Whys")**". Why reveals either for what reason or purpose something is the way it is or illustrates the relationship between two events. It's a fundamental building block in knowledge crunching and the distillation process. Don't ask me why, but for me to build the right thing it's of the utmost importance that I understand things first. Why is my shovel in digging for axioms. And like anything in business, axioms might shift into mutation.

### Communication

It's very easy for conversations with people knowledgable about the domain to go technical, to focus on the wrong area, to focus on the minute details instead of the bigger picture or vice versa, to use abstract terms where you think you get it but actually you don't, to use an unwritten glossary without a sense of meaning or terminology deduplication, ... How do you deal with that? You make things explicit. Boundaries, expectations, glossary, abstractions understood by all, to name a few. To use some of [Eric Evan's](https://twitter.com/ericevans0 "Eric Evans") words: You cultivate a language, together (Ingredients of Effective Modeling, p12, Chapter 1: Crunching knowledge). Yet words can only take us so far. Salvation, for me, comes from the cross-fertilization of the written/spoken word with visualization techniques. This should sound familiar. It's the thing you do when you're standing in front of a white board and you draw a little and you talk a little and you draw a little more, you scratch something, you engage in a discussion but try to stay focused, you simplify, you explore an alternate design, etc.

> If you don't want it to turn into a one-man show, just hand everybody a marker. There, solved it.

Over time a remarkable thing might start to happen: you're drawing the same thing over and over again. Probably not exactly the same, sometimes only a particular part stays the same, sometimes it looks like you could decompose it into a bunch of reusable building blocks. I doubt many will notice unless they're very reflective about what they do. Whenever this happens, it might be time to bring on the creativity and craft yourself a tangible, visual [domain specific language](http://en.wikipedia.org/wiki/Domain-specific_language "Domain specific language") (DSL).

### A visual DSL

Although building a tangible, visual DSL is not a particularly costly undertaking, I do think there should be some return on investment. It's waste when you can't put it to good use, no? So, what do you need to create one? If we take it back to basics, often some paper or cardboard, a pair of scissors and some glue will do. You can get way more sophisticated than that: printable stencils, shapes emitted from your 3D printer, [legos](http://www.lego.com/ "Lego"), ... your imagination is the limit, as long as it doesn't become about the tool rather than what it enables, i.e. better communication. Like with many things, experimentation is key. Let me whip up a little sample of what that all looked like.

#### An example

During my conversations with domain experts, I noticed I drew the notion of a timeline over and over again. Now, the timeline itself, below reduced to the scope of a day, was not the center piece of the conversation. It was more like a context or setting in which the conversation took place. The meaningful scenarios would all take place inside this virtual box. [<img src="http://seabites.files.wordpress.com/2014/01/img_3868.jpg?w=630" alt="Timeline" class="size-large wp-image-697" width="630" height="843" />](img_3870.jpg) Now, we weren't just dealing with appointments, we'd have blocked periods and unavailabilities as well(\*). To make that difference more explicit, I'd use colour overlays like shown below, although coloured paper would 've had the same effect. Adding a little legend - and sticking to it - made it easy for us all to have a conversation - now and in the future - about real life scenarios that involved these different concepts.

> (\*): don't worry if you don't get the difference, it's domain specific anyway.

[<img src="http://seabites.files.wordpress.com/2014/01/img_3872.jpg" alt="IMG_3872" class="aligncenter size-full wp-image-699" width="630" height="843" />](img_3876.jpg)

### In retrospect

The most useful aspect, by far, was that you could create photo stills of before and after situations - I doubt there's a faster way to document scenarios. Gradually, scenarios evolved into "given, when, then", which meshed well with how we were going to test them using code. Still, you had to be there to get what the scenarios were about. It wasn't a substitute for communication. It was an enabler of efficient, terse, to the point conversation. When in doubt about a certain scenario, I could pull out a deck of cards and a piece of paper and I'd have my answer within minutes, not hours or days. I guess, to me, at the time, that's were the value was, in developing a common language, aided by a useful model that kept the conversation focused. Over time, some of these visual DSLs would fade, having proved their usefulness when a feature was complete. That's okay, it's a tool, not an end.
