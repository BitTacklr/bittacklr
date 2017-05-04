---
original: https://seabites.wordpress.com/2010/10/31/guards-and-queries-in-the-domain-model/
title: "Guards and Queries in the domain model"
slug: "guards-and-queries-in-the-domain-model"
date: 2010-10-31
author: Yves Reynhout
tags: [ddd]
publish: true
---
Once you embrace that getters/setters are an anti-pattern in the domain model, there are other things that start becoming more difficult, at least at first glance. How are you going to enforce business rules in the domain model when you can't query state using property getters? All too often people revert to enforcing a rule by querying an entity/aggregate for its state (using fields or properties), breaking encapsulation, causing yet another dependency. When multiple aggregates are used to establish a collaboration (e.g. an event or a transaction) this kind of behavior is probably reenforced. Yet the solution is very straightforward. You introduce what I call Guard methods in your domain model. They should communicate their intent, i.e. the functionality they guard. In [SOM](http://www.informit.com/store/product.aspx?isbn=0130668397 "Streamlined Object Modeling") these are called test methods, but given the multiple connotations of the word test, I chose guard as its replacement. 

```csharp
 public class Order : Aggregate { public Order Place(Customer customer, ...) { customer.GuardPlaceOrder(); //... } } public class Customer : Aggregate { private bool \_hasMoreThanFiveOutstandingInvoices; internal void GuardPlaceOrder() { //The kind of rule(s) that go in here might change over time. Guard.Against(\_hasMoreThanFiveOutstandingInvoices, PlaceOrderErrorCode.CustomerHasMoreThanFiveOutstandingInvoices); } } public static class Guard { public static void Against&lt;TErrorCode&gt;(bool assertion, TErrorCode errorCode) where TErrorCode : IConvertible { if(assertion) throw new OperationException(errorCode); } } 
```

 A very handy side-effect of guards is that the rules governing one functionality do no get mixed with other functionality. It also becomes obvious where to code the rule: in the guard member of the aggregate that has the state required to make the decision. Another advantage is the contextual error throwing. Even though you might visit two or more aggregates to establish/dissolve a collaboration, when you throw, you only have one thing to catch and you know what rule was violated (by giving it a name). This makes for a very powerful domain to commandhandler exception communication mechanism. Sometimes you just need to compare state that lives in different aggregates. That's when queries come in handy. 

```csharp
 public class Trash : Aggregate { private string \_countryOfOrigin; public void DumpAt(Place dumpster, ...) { Guard.Against(!dumpster.IsInSameCountryAs(\_countryOfOrigin), DumpTrashAtError.TrashCannotBeDumpedOutsideTheCountryOfOrigin); //... } } public class Place : Aggregate { private string \_country; //Your query method will be more sophisticated. internal bool IsInSameCountryAs(string otherCountry) { return \_country.Equals(otherCountry); } } 
```

 One might say that these query methods are glorified property getters. That may be so, still I think query methods (determine mine services) communicate intent better, transcend the "one property" notion and encapsulate the "how" better.  Additionally they shouldn't alter state! I haven't found any rule I couldn't implement using these two constructs, my domain model has become much more readable, and the place where "rules" go in code has become clearer. As an added bonus, command handler exception handling has become a matter of mapping a domain error code onto a command error code.
