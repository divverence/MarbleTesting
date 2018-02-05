# Divverence.MarbleTesting.Akka

Divverence.MarbleTesting.Akka is a small library that allows to write tests for [Akka.Net](https://github.com/akkadotnet/akka.net/) actors / systems using marble diagrams in text form.
This library is inspired by the practice in the Rx / ReactiveStreams world to use [marble diagrams](http://rxmarbles.com/) to describe the (intented) behaviour of operators.
It's our belief that this concept also applies very nicely to [Actors](https://petabridge.com/blog/akkadotnet-what-is-an-actor/) in an [actor model](https://en.wikipedia.org/wiki/Actor_model).
Inspiration for this library came from the ideas of [Erik Meijer](https://twitter.com/headinthebox) and the [marble test features](https://github.com/ReactiveX/rxjs/blob/master/doc/writing-marble-tests.md) of RxJS v5.

The purpose of the library is to help you write unit tests for your [Akka.Net](https://github.com/akkadotnet/akka.net/) actors that are as readable and consice as possible.

For background on Marble Testing in general, check this nice [7 minutes introduction to RxJs testing](https://egghead.io/lessons/rxjs-introduction-to-rxjs-marble-testing) on egghead.io.

This library is complementary to [MarbleTest.Net](https://github.com/alexvictoor/MarbleTest.Net), which targets System.Reactive (Rx.Net) specifically. Thanks go out to the autor(s) of that project for ideas and possibly some source code.

## Getting Started

You can use `Divverence.MarbleTesting` in .NET and .Net Core 2 - the nuget package is available for `net452` and `netstardard`.

To get the lib just use nuget, from the nuget package console in Visual Studio:

```posh
PM> Install-Package Divverence.MarbleTesting
PM> Install-Package Divverence.MarbleTesting.Akka
```

Or add it with the dotnet CLI from `cmd`, `powershell` or `bash`:

```sh
dotnet add package Divverence.MarbleTesting.Akka
```

## Usage

Use pieces of code like this in your unit tests for you actors:

```csharp
var marbleTest = new AkkaMarbleTest( ActorSystem );
marbleTest.WhenTelling("       --a---a---b--a-c--", disctinctActor, s => s);
marbleTest.ExpectMsgs<string>("--a-------b----c--", s => o => o.Should().Be(s));
await marbleTest.Run();
```

```csharp
var marbleTest = new AkkaMarbleTest( Sys );
var throttleActor = Sys.ActorOf( ThrottleActor.Props(TimeSpan.FromSeconds(5)) );
marbleTest.WhenTelling("       --a---a-b-----c---------(d,e)---", throttleActor, s => s);
marbleTest.ExpectMsgs<string>("--a----a----b----c------d----e--", s => o => o.Should().Be(s));
var virtualTimeStep = TimeSpan.FromSeconds(1);
await marbleTest.Run(virtualTimeStep); // This runs in milliseconds!
```

## Building

Use dotnet CLI 2.0:

```sh
dotnet build -c Release
```

## Marble Sequence syntax

The syntax for describing Sequences is compatible with the RxJS syntax.
Each character represents what happens during a moment in virtual time.

**'-'** means that no message is sent/received at that moment
**'a'** or any other non-reserved character means that a message is sent or received at that moment

So in the 'Tell' usage, "X-Y-" means:

- At time '0', a message 'X' is sent
- At time '1', nothing is sent
- At time '2', a message 'Y' is sent
- At time '3', nothing is sent
- Before and after time '0' and '3', nothing is sent

And in the 'Expect' usage, "X-Y-" means:

- At time '0', a message 'X' should arrive and nothing else
- At time '1', no message should arrive
- At time '2', a message 'Y' should arrive and nothing else
- At time '3', no message should arrive
- Before time '0', nothing should arrive
- After time '3', no assertions are made about what should happen.

### Simultaneous messages

If messages should be sent or expected simultaneously, you can group them using paranthesis.
So "--(abc)--" means events a, b and c occur at moment 2.

For a complete description of the syntax, please refer to the [official RxJS documentation](https://github.com/ReactiveX/rxjs/blob/master/doc/writing-marble-tests.md)

## Supported Test Frameworks

Divverence.MarbleTesting.Akka internally uses the TestKitBase.Expect(No)Msg(s) methods for all assertions, and should therefore work with any of the test frameworks supported by Akka.Net.
