---
original: https://seabites.wordpress.com/2013/05/30/eventbuilders-revisited/
title: "EventBuilders - Revisited"
slug: "eventbuilders-revisited"
date: 2013-05-30
author: Yves Reynhout
publish: true
---
### Introduction

I've been using [event builders](http://seabites.wordpress.com/2011/07/27/eventbuilders/ "EventBuilders") for some time now. With time and practice comes experience (at least that's the plan), both good and bad. Investing in event or - more general - message builders means first and foremost investing in *language*. If you're not willing to put in that effort, don't bother using them, at least not for the purpose of spreading them around your entire codebase. That piece of distilled wisdom applies equally to messages themselves, as far as I'm concerned. Builders are appealing because, if you do make the investment in embedding proper language, they can make your code a bit more readable. Granted, in a language like C\# (at least the recent versions), that might have become less of a concern since things like [object initializers](http://msdn.microsoft.com/en-us/library/vstudio/bb384062.aspx "Object Initializers") and [named arguments](http://msdn.microsoft.com/en-us/library/vstudio/dd264739.aspx "Named Arguments") can go a long way to improve readability. Builders are very similar to [Test Data Builders](http://www.natpryce.com/articles/000714.html "Test Data Builders"). In fact, if you're writing test specifications that use some sort of *Given-When-Then* syntax, builders can be useful to take the verbosity out of those specifications. You can tuck away test-specific, pre-initialized builders behind properties or behind test-suite specific classes. A lesser known feature of builders (especially the mutable kind - more about that below) is that you can pass them around, getting them to act as data collectors, because the state they need might not be available at their construction site(\*). A well crafted model might rub this even more in your face (think about all the data in your value objects, entities, etc ...). If you take a more *functional* approach - the fashionable thing to do these days - to passing them around, you can use immutable builders and hand back a new copy with the freshly collected values. Messages, as in *their representation in code*, go hand in hand with serialization. There are many ways of doing serialization, with hand rolled, code generated and reflection based being the predominant ones. Sometimes the serialization library you depend upon comes with its own quirks. At that point, builders could be useful to insulate the rest of your code from having to know about those quirks or how to handle them. Whether you really need another "abstraction" sitting in between is debatable. Composition is often overlooked when defining messages, resulting in flat, dictionary-like *data dumpsters*. Yet both json and xml - probably the most predominant textual serialization formats - allow by their very nature to define information in a hierarchic way, ergo composing messages from smaller bits of information. Not that I particularly believe that doing so is tied to the choosen serialization format. This is another area builders can help since they could be conceived around these highly cohesive bits of information, at least if your model has a similar shape (not that it has to). Message builders could then leverage message part builders or code could use message part builders to feed the proper data into message builders. This ties in nicely with the builder being a data collector. (\*) Construction site, i.e. the place where they are created.

### A world of flavors

Below I'll briefly glance over various flavors I've used myself or seen floating around. This post is a bit heavy on the (C\#) code side of things and repetitive (on purpose, I might add). For demonstration purposes, I've shamelessly stolen and mutated a bit of [code](https://github.com/mspnp/cqrs-journey-code/blob/master/source/Conference/Conference.Contracts/SeatCreated.cs "Seat Created Event") from [The CQRS Journey](http://cqrsjourney.github.io/ "The CQRS Journey").

#### Flavor 1 - Mutable event with getters and setters - no builder



```csharp
 namespace Flavor1\_MutableEvent\_GettersNSetters\_NoBuilder { public class SeatTypeCreated { public Guid ConferenceId { get; set; } public Guid SeatTypeId { get; set; } public string Name { get; set; } public string Description { get; set; } public int Quantity { get; set; } // Optional ctor (if you're not happy with the datatype defaults) public SeatTypeCreated() { ConferenceId = Guid.Empty; SeatTypeId = Guid.Empty; Name = string.Empty; Description = string.Empty; Quantity = 0; } public override string ToString() { return string.Format("New seat type created '{0}' ({1}) for conference '{2}': {3}. Initial seating quantity is {4}.", Name, SeatTypeId, ConferenceId, Description, Quantity); } } public static class SampleUsage { public static void Show() { var \_ = new SeatTypeCreated { ConferenceId = Guid.NewGuid(), SeatTypeId = Guid.NewGuid(), Name = "Terrace Level", Description = "Luxurious, bubblegum stain free seats.", Quantity = 25 }; Console.WriteLine(\_); \_.Quantity = 35; Console.WriteLine(\_); } } } 
```

 This is what I call the *I'm in a hurry* version where you don't see nor feel the need to have builders and you're not particularly worried about events/messages getting mutated. It's still pretty descriptive due to the object intializers. There's a time and place for everything.

> "[Mutable messages are an anti-pattern](http://codebetter.com/gregyoung/2008/04/15/dddd-4-messages-are-value-objects/ "Mutable messages are an anti-pattern"). They are the path to a system that is held together with duct tape and bubble gum." - Greg Young anno 2008.

I don't know if Greg still feels as strongly about it today. You'll have to ask him. Great quoting material is all I can say.

#### Flavor 2 - Immutable event with getters and readonly fields - no builder



```csharp
 namespace Flavor2\_ImmutableEvent\_GettersNReadOnlyFields\_NoBuilder { public class SeatTypeCreated { readonly Guid \_conferenceId; readonly Guid \_seatTypeId; readonly string \_name; readonly string \_description; readonly int \_quantity; public Guid ConferenceId { get { return \_conferenceId; } } public Guid SeatTypeId { get { return \_seatTypeId; } } public string Name { get { return \_name; } } public string Description { get { return \_description; } } public int Quantity { get { return \_quantity; } } public SeatTypeCreated(Guid conferenceId, Guid seatTypeId, string name, string description, int quantity) { \_conferenceId = conferenceId; \_seatTypeId = seatTypeId; \_name = name; \_description = description; \_quantity = quantity; } public override string ToString() { return string.Format("New seat type created '{0}' ({1}) for conference '{2}': {3}. Initial seating quantity is {4}.", \_name, \_seatTypeId, \_conferenceId, \_description, \_quantity); } } public static class SampleUsage { public static void Show() { var \_ = new SeatTypeCreated( conferenceId: Guid.NewGuid(), seatTypeId: Guid.NewGuid(), name: "Terrace Level", description: "Luxurious, bubblegum stain free seats.", quantity: 25 ); Console.WriteLine(\_); var \_\_ = new SeatTypeCreated(\_.ConferenceId, \_.SeatTypeId, \_.Name, \_.Description, 35); Console.WriteLine(\_\_); } } } 
```

 This is the immutable companion to the previous one. Named arguments cater for the readability in this one. A bit heavy on the typing if you want to mutate the event. It also implies you collect **ALL** information before you're able to construct it.

#### Flavor 3 - Mutable event with getters and setters - implicit builder



```csharp
 namespace Flavor3\_MutableEvent\_GettersNSetters\_ImplicitBuilder { public class SeatTypeCreated { public Guid ConferenceId { get; set; } public Guid SeatTypeId { get; set; } public string Name { get; set; } public string Description { get; set; } public int Quantity { get; set; } public SeatTypeCreated AtConference(Guid identifier) { ConferenceId = identifier; return this; } public SeatTypeCreated IdentifiedBy(Guid identifier) { SeatTypeId = identifier; return this; } public SeatTypeCreated Named(string value) { Name = value; return this; } public SeatTypeCreated DescribedAs(string value) { Description = value; return this; } public SeatTypeCreated WithInitialQuantity(int value) { Quantity = value; return this; } // Optional ctor (if you're not happy with the datatype defaults) public SeatTypeCreated() { ConferenceId = Guid.Empty; SeatTypeId = Guid.Empty; Name = string.Empty; Description = string.Empty; Quantity = 0; } public override string ToString() { return string.Format("New seat type created '{0}' ({1}) for conference '{2}': {3}. Initial seating quantity is {4}.", Name, SeatTypeId, ConferenceId, Description, Quantity); } } public static class SampleUsage { public static void Show() { var \_ = new SeatTypeCreated { Quantity = 35 //does not do what you expect ... }. AtConference(Guid.NewGuid()). IdentifiedBy(Guid.NewGuid()). Named("Terrace Level"). DescribedAs("Luxurious, bubblegum stain free seats."). WithInitialQuantity(25); Console.WriteLine(\_); \_.Quantity = 35; Console.WriteLine(\_); } } } 
```

 This is what I call the "AWS" variety since it's what is being used in [Amazon's SDK for .NET](http://aws.amazon.com/sdkfornet/ "Amazon SDK for .NET"). You get the readability of the builder with methods right on the message object itself and after each method you have access to the mutable instance of the message. What's not to like, except for the mutability?

#### Flavor 4 - Mutable event with getters and setters - explicit builder



```csharp
 namespace Flavor4\_MutableEvent\_GettersNSetters\_ExplicitBuilder { public class SeatTypeCreated { public Guid ConferenceId { get; set; } public Guid SeatTypeId { get; set; } public string Name { get; set; } public string Description { get; set; } public int Quantity { get; set; } // Optional ctor (if you're not happy with the datatype defaults) public SeatTypeCreated() { ConferenceId = Guid.Empty; SeatTypeId = Guid.Empty; Name = string.Empty; Description = string.Empty; Quantity = 0; } // Optional convenience method public SeatTypeCreatedBuilder ToBuilder() { return new SeatTypeCreatedBuilder(). AtConference(ConferenceId). IdentifiedBy(SeatTypeId). Named(Name). DescribedAs(Description). WithInitialQuantity(Quantity); } public override string ToString() { return string.Format("New seat type created '{0}' ({1}) for conference '{2}': {3}. Initial seating quantity is {4}.", Name, SeatTypeId, ConferenceId, Description, Quantity); } } public class SeatTypeCreatedBuilder { Guid \_conferenceId; Guid \_seatTypeId; string \_name; string \_description; int \_quantity; // Optional ctor (if you're not happy with the datatype defaults) public SeatTypeCreatedBuilder() { \_conferenceId = Guid.Empty; \_seatTypeId = Guid.Empty; \_name = string.Empty; \_description = string.Empty; \_quantity = 0; } public SeatTypeCreatedBuilder AtConference(Guid identifier) { \_conferenceId = identifier; return this; } public SeatTypeCreatedBuilder IdentifiedBy(Guid identifier) { \_seatTypeId = identifier; return this; } public SeatTypeCreatedBuilder Named(string value) { \_name = value; return this; } public SeatTypeCreatedBuilder DescribedAs(string value) { \_description = value; return this; } public SeatTypeCreatedBuilder WithInitialQuantity(int value) { \_quantity = value; return this; } public SeatTypeCreated Build() { return new SeatTypeCreated { ConferenceId = \_conferenceId, SeatTypeId = \_seatTypeId, Name = \_name, Description = \_description, Quantity = \_quantity }; } } public static class SampleUsage { public static void Show() { var \_ = new SeatTypeCreatedBuilder(). AtConference(Guid.NewGuid()). IdentifiedBy(Guid.NewGuid()). Named("Terrace Level"). DescribedAs("Luxurious, bubblegum stain free seats."). WithInitialQuantity(25). Build(); Console.WriteLine(\_); \_.Quantity = 35; Console.WriteLine(\_); var \_\_ = \_.ToBuilder(). WithInitialQuantity(45). Build(); Console.WriteLine(\_\_); } } } 
```

 This is just a simple variation on the above with the builder pulled out of the message object. Might come in handy if you don't have control over the messages but you still fancy builders (the ToBuilder *could* become an extension method in that case).

#### Flavor 5 - Immutable event with getters and readonly fields - implicit builder



```csharp
 namespace Flavor5\_ImmutableEvent\_GettersNReadOnlyFields\_ImplicitBuilder { public class SeatTypeCreated { readonly Guid \_conferenceId; readonly Guid \_seatTypeId; readonly string \_name; readonly string \_description; readonly int \_quantity; public Guid ConferenceId { get { return \_conferenceId; } } public Guid SeatTypeId { get { return \_seatTypeId; } } public string Name { get { return \_name; } } public string Description { get { return \_description; } } public int Quantity { get { return \_quantity; } } public SeatTypeCreated() { \_conferenceId = Guid.Empty; \_seatTypeId = Guid.Empty; \_name = string.Empty; \_description = string.Empty; \_quantity = 0; } SeatTypeCreated(Guid conferenceId, Guid seatTypeId, string name, string description, int quantity) { \_conferenceId = conferenceId; \_seatTypeId = seatTypeId; \_name = name; \_description = description; \_quantity = quantity; } public SeatTypeCreated AtConference(Guid identifier) { return new SeatTypeCreated(identifier, \_seatTypeId, \_name, \_description, \_quantity); } public SeatTypeCreated IdentifiedBy(Guid identifier) { return new SeatTypeCreated(\_conferenceId, identifier, \_name, \_description, \_quantity); } public SeatTypeCreated Named(string value) { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, value, \_description, \_quantity); } public SeatTypeCreated DescribedAs(string value) { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, \_name, value, \_quantity); } public SeatTypeCreated WithInitialQuantity(int value) { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, \_name, \_description, value); } public override string ToString() { return string.Format("New seat type created '{0}' ({1}) for conference '{2}': {3}. Initial seating quantity is {4}.", \_name, \_seatTypeId, \_conferenceId, \_description, \_quantity); } } public static class SampleUsage { public static void Show() { var \_ = new SeatTypeCreated(). AtConference(Guid.NewGuid()). IdentifiedBy(Guid.NewGuid()). Named("Terrace Level"). DescribedAs("Luxurious, bubblegum stain free seats."). WithInitialQuantity(25); Console.WriteLine(\_); var \_\_ = \_.WithInitialQuantity(35); Console.WriteLine(\_\_); } } } 
```

 This is an immutable version of the "AWS" variety, giving you a new event upon each call. Great if you intend to have different branches off of the same message, such as for testing purposes. Pushes you down the functional alley if you want to collect data since each mutation gives you a new instance. It's probably my favorite when dealing with flat datastructures. I'm no expert, but I'm pretty sure the immutable versions are going to use more memory (or at least annoy GC's GEN 0). Whether that's something you should be overly focused on highly depends on your particular context.

> "In the land of IO, the blind man, who doesn't measure, optimizes for the wrong thing first." - Yves anno 2013

#### Flavor 6 - Immutable event with getters and readonly fields - Mutable explicit builder



```csharp
 namespace Flavor6\_ImmutableEvent\_GettersNReadOnlyFields\_MutableExplicitBuilder { public class SeatTypeCreated { readonly Guid \_conferenceId; readonly Guid \_seatTypeId; readonly string \_name; readonly string \_description; readonly int \_quantity; public Guid ConferenceId { get { return \_conferenceId; } } public Guid SeatTypeId { get { return \_seatTypeId; } } public string Name { get { return \_name; } } public string Description { get { return \_description; } } public int Quantity { get { return \_quantity; } } public SeatTypeCreated(Guid conferenceId, Guid seatTypeId, string name, string description, int quantity) { \_conferenceId = conferenceId; \_seatTypeId = seatTypeId; \_name = name; \_description = description; \_quantity = quantity; } // Optional convenience method public SeatTypeCreatedBuilder ToBuilder() { return new SeatTypeCreatedBuilder(). AtConference(ConferenceId). IdentifiedBy(SeatTypeId). Named(Name). DescribedAs(Description). WithInitialQuantity(Quantity); } public override string ToString() { return string.Format("New seat type created '{0}' ({1}) for conference '{2}': {3}. Initial seating quantity is {4}.", \_name, \_seatTypeId, \_conferenceId, \_description, \_quantity); } } public class SeatTypeCreatedBuilder { Guid \_conferenceId; Guid \_seatTypeId; string \_name; string \_description; int \_quantity; // Optional ctor (if you're not happy with the datatype defaults) public SeatTypeCreatedBuilder() { \_conferenceId = Guid.Empty; \_seatTypeId = Guid.Empty; \_name = string.Empty; \_description = string.Empty; \_quantity = 0; } public SeatTypeCreatedBuilder AtConference(Guid identifier) { \_conferenceId = identifier; return this; } public SeatTypeCreatedBuilder IdentifiedBy(Guid identifier) { \_seatTypeId = identifier; return this; } public SeatTypeCreatedBuilder Named(string value) { \_name = value; return this; } public SeatTypeCreatedBuilder DescribedAs(string value) { \_description = value; return this; } public SeatTypeCreatedBuilder WithInitialQuantity(int value) { \_quantity = value; return this; } public SeatTypeCreated Build() { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, \_name, \_description, \_quantity); } } public static class SampleUsage { public static void Show() { var \_ = new SeatTypeCreatedBuilder(). AtConference(Guid.NewGuid()). IdentifiedBy(Guid.NewGuid()). Named("Terrace Level"). DescribedAs("Luxurious, bubblegum stain free seats."). WithInitialQuantity(25). Build(); Console.WriteLine(\_); var builder = \_.ToBuilder(); var \_\_ = builder. Named("Balcony level"). DescribedAs("High end seats. No smoking policy."). WithInitialQuantity(45). Build(); Console.WriteLine(\_\_); var \_\_\_ = builder. WithInitialQuantity(45). Build(); //Probably not the result you expect Console.WriteLine(\_\_\_); } } } 
```

 This kind of builder is great if you want to collect data using the same instance before producing the immutable message.

#### Flavor 7 - Immutable event with getters and readonly fields - Immutable explicit builder



```csharp
 namespace Flavor7\_ImmutableEvent\_GettersNReadOnlyFields\_ImmutableExplicitBuilder { public class SeatTypeCreated { readonly Guid \_conferenceId; readonly Guid \_seatTypeId; readonly string \_name; readonly string \_description; readonly int \_quantity; public Guid ConferenceId { get { return \_conferenceId; } } public Guid SeatTypeId { get { return \_seatTypeId; } } public string Name { get { return \_name; } } public string Description { get { return \_description; } } public int Quantity { get { return \_quantity; } } public SeatTypeCreated(Guid conferenceId, Guid seatTypeId, string name, string description, int quantity) { \_conferenceId = conferenceId; \_seatTypeId = seatTypeId; \_name = name; \_description = description; \_quantity = quantity; } // Optional convenience method public SeatTypeCreatedBuilder ToBuilder() { return new SeatTypeCreatedBuilder(). AtConference(ConferenceId). IdentifiedBy(SeatTypeId). Named(Name). DescribedAs(Description). WithInitialQuantity(Quantity); } public override string ToString() { return string.Format("New seat type created '{0}' ({1}) for conference '{2}': {3}. Initial seating quantity is {4}.", \_name, \_seatTypeId, \_conferenceId, \_description, \_quantity); } } public class SeatTypeCreatedBuilder { readonly Guid \_conferenceId; readonly Guid \_seatTypeId; readonly string \_name; readonly string \_description; readonly int \_quantity; // Optional ctor content (if you're not happy with the datatype defaults) public SeatTypeCreatedBuilder() { \_conferenceId = Guid.Empty; \_seatTypeId = Guid.Empty; \_name = string.Empty; \_description = string.Empty; \_quantity = 0; } SeatTypeCreatedBuilder(Guid conferenceId, Guid seatTypeId, string name, string description, int quantity) { \_conferenceId = conferenceId; \_seatTypeId = seatTypeId; \_name = name; \_description = description; \_quantity = quantity; } public SeatTypeCreatedBuilder AtConference(Guid identifier) { return new SeatTypeCreatedBuilder(identifier, \_seatTypeId, \_name, \_description, \_quantity); } public SeatTypeCreatedBuilder IdentifiedBy(Guid identifier) { return new SeatTypeCreatedBuilder(\_conferenceId, identifier, \_name, \_description, \_quantity); } public SeatTypeCreatedBuilder Named(string value) { return new SeatTypeCreatedBuilder(\_conferenceId, \_seatTypeId, value, \_description, \_quantity); } public SeatTypeCreatedBuilder DescribedAs(string value) { return new SeatTypeCreatedBuilder(\_conferenceId, \_seatTypeId, \_name, value, \_quantity); } public SeatTypeCreatedBuilder WithInitialQuantity(int value) { return new SeatTypeCreatedBuilder(\_conferenceId, \_seatTypeId, \_name, \_description, value); } public SeatTypeCreated Build() { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, \_name, \_description, \_quantity); } } public static class SampleUsage { public static void Show() { var \_ = new SeatTypeCreatedBuilder(). AtConference(Guid.NewGuid()). IdentifiedBy(Guid.NewGuid()). Named("Terrace Level"). DescribedAs("Luxurious, bubblegum stain free seats."). WithInitialQuantity(25). Build(); Console.WriteLine(\_); var builder = \_.ToBuilder(); var \_\_ = builder. Named("Balcony level"). DescribedAs("High end seats. No smoking policy."). WithInitialQuantity(45). Build(); Console.WriteLine(\_\_); var \_\_\_ = builder. WithInitialQuantity(45). Build(); //The result you expect Console.WriteLine(\_\_\_); } } } 
```

 This one is only useful in the odd case you'd like to do branching off of the builder. It might be worthy to read [Greg's musings](http://codebetter.com/gregyoung/2008/04/16/dddd-6-fluent-builders-alternate-ending/ "Alternate ending for fluent builders") on this and the previous one.

#### Flavor 8 - Immutable event with getters and readonly fields - Implicit builder combined with mutable explicit builder



```csharp
 namespace Flavor8\_ImmutableEvent\_GettersNReadOnlyFields\_ImplicitBuilderCombinedWithMutableExplicitBuilder { public class SeatTypeCreated { readonly Guid \_conferenceId; readonly Guid \_seatTypeId; readonly string \_name; readonly string \_description; readonly int \_quantity; public Guid ConferenceId { get { return \_conferenceId; } } public Guid SeatTypeId { get { return \_seatTypeId; } } public string Name { get { return \_name; } } public string Description { get { return \_description; } } public int Quantity { get { return \_quantity; } } public SeatTypeCreated() { \_conferenceId = Guid.Empty; \_seatTypeId = Guid.Empty; \_name = string.Empty; \_description = string.Empty; \_quantity = 0; } SeatTypeCreated(Guid conferenceId, Guid seatTypeId, string name, string description, int quantity) { \_conferenceId = conferenceId; \_seatTypeId = seatTypeId; \_name = name; \_description = description; \_quantity = quantity; } public SeatTypeCreated AtConference(Guid identifier) { return new SeatTypeCreated(identifier, \_seatTypeId, \_name, \_description, \_quantity); } public SeatTypeCreated IdentifiedBy(Guid identifier) { return new SeatTypeCreated(\_conferenceId, identifier, \_name, \_description, \_quantity); } public SeatTypeCreated Named(string value) { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, value, \_description, \_quantity); } public SeatTypeCreated DescribedAs(string value) { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, \_name, value, \_quantity); } public SeatTypeCreated WithInitialQuantity(int value) { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, \_name, \_description, value); } public Builder ToBuilder() { return new Builder(). AtConference(ConferenceId). IdentifiedBy(SeatTypeId). Named(Name). DescribedAs(Description). WithInitialQuantity(Quantity); } public override string ToString() { return string.Format("New seat type created '{0}' ({1}) for conference '{2}': {3}. Initial seating quantity is {4}.", \_name, \_seatTypeId, \_conferenceId, \_description, \_quantity); } public class Builder { Guid \_conferenceId; Guid \_seatTypeId; string \_name; string \_description; int \_quantity; // Optional ctor (if you're not happy with the datatype defaults) internal Builder() { \_conferenceId = Guid.Empty; \_seatTypeId = Guid.Empty; \_name = string.Empty; \_description = string.Empty; \_quantity = 0; } public Builder AtConference(Guid identifier) { \_conferenceId = identifier; return this; } public Builder IdentifiedBy(Guid identifier) { \_seatTypeId = identifier; return this; } public Builder Named(string value) { \_name = value; return this; } public Builder DescribedAs(string value) { \_description = value; return this; } public Builder WithInitialQuantity(int value) { \_quantity = value; return this; } public SeatTypeCreated Build() { return new SeatTypeCreated(\_conferenceId, \_seatTypeId, \_name, \_description, \_quantity); } } } public static class SampleUsage { public static void Show() { var \_ = new SeatTypeCreated(). AtConference(Guid.NewGuid()). IdentifiedBy(Guid.NewGuid()). Named("Terrace Level"). DescribedAs("Luxurious, bubblegum stain free seats."). WithInitialQuantity(25); Console.WriteLine(\_); var builder = \_.ToBuilder(); var \_\_ = builder. Named("Balcony level"). DescribedAs("High end seats. No smoking policy."). WithInitialQuantity(45). Build(); Console.WriteLine(\_\_); var \_\_\_ = \_. WithInitialQuantity(45); //The result you expect Console.WriteLine(\_\_\_); } } } 
```

 A slight variation that may prove useful if you know by convention that builders are mutable and events/messages are not. This would allow you to pass a builder around whenever opportunity knocks and get back to the immutable shape when you're done using the builder. I'll spare you flavor 9 which combines explicit mutable and immutable builders with implicit builders.

> Congratulations, achievement unlocked "*scrolled to the end*".

### Conclusion

By no means is this list of flavors finite. Other programming languages might have constructs which make authoring this way easier. Overall, I'm quite content and comfortable with using builders, having shifted somewhat more to the immutable side of the flavor spectrum. I'm pretty sure they're not for everybody, and that's just fine. The choice of which flavor to use highly depends on the scenario at hand, how comfortable a team is with them in general, what benefits could be gotten from them. I also feel you shouldn't try to force the same type of builder on all events/messages since simplicity might not warrant their *perceived* complexity. A consistent approach is not something I'm particularly fond of since it's mainly driven by the human desire to do everything the same way, not by rationalizing about what would be the best choice in a particular situation. Then again, I probably think too much - pretty sure some will think I've gone bananas. Maybe I have ... what a way to end a post. Later!