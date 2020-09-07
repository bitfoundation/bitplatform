[![NuGet version](https://badge.fury.io/nu/Bit.Simila.svg)](https://badge.fury.io/nu/Bit.Simila)

# Installing via NuGet
```powershell
Install-Package Bit.Simila
```

# What is Simila?
Are **Color** and **Colour** equal? No!

```c#
if ("Color" == "Coluor")
   // Always false

if ("The Candy Shop" == "The Kandi Schap")
   // Always false
```

But they **are Similar** in **Simila**!

```c#
if (simila.AreSimilar("Color", "Colour"))
   // It's true now!

if (simila.AreSimilar("The Candy Shop", "The Kandi Schap"));
   // It's true now!
```
# How to use
```c#
var simila = new Simila();

// Comparing Words
simila.AreSimilar("Lamborghini", "Lanborgini"); // True


// Comparing Expressions
simila.AreSimilar("Lamborghini is some great car", "Lanborgini is some graet kar"); // True
```
## Customizing Simila 

### **Treshold**
You set the sensivity of similarity by setting `Treshold`. If not set, default value is `0.6` which means it considers similar if they are `60%` similar

```c#
// Are similar if their at least 50% similar.
var similaEasy = new Simila()
{
    Treshold = 0.5 
};

// considered as similar.
similaEasy.IsSimilar("Lamborghini", "Lanborgni"); // True, They are 50% similar.

// Are similar if their at least 80% similar.
var similaTough = new Simila() 
{ 
    Treshold = 0.8 
};

// considered as NOT similar!
similaEasy.AreSimilar("Lamborghini", "Lanborgni"); // False, Not 80% similar.
```

### Similarity Resolver
Similarity Resolvers are different **algorithms** which Simila can use for similarity checking. 
Each algorithm works fine it is being used in its proper scenario.

There are 3 types of similarity resolvers available in Simila:
 - **Levenshtein (Default)**: It works good if we need them to **look similar**. You can read more about Levenshtein here: [Levenshtein Algorithm](https://en.wikipedia.org/wiki/Levenshtein_distance)
 - **Soundex:** It works good if we need them to **sound similar**. You can read more about Soundex here: [Soundex Algorithm](https://en.wikipedia.org/wiki/Soundex)
 - **SharedPair:** It works good if we need them to **structured similar**.
 
 You can configure simila to use a specific algorithm. We call them Resolvers.
 
 #### Using Soudex Resolver
 ```c#
var similaSounedx = new Simila()
{
    Resolver = new SoundexSimilarityResolver()
};
```

#### Using SharedPair Resolver
```c#
var similaSharedPair = new Simila()
{
    Resolver = new SharedPairSimilarityResolver()
};
```

#### Using Levenshtein Resolver
Levenshtein is even more configurable. You can set the accepted mistakes both character level and word level.
In this example we told Simila to consider `color` and `colour` words similar.
```c#
 var simila = new Simila()
 {
     Resolver = new PhraseSimilarityResolver(
                  new WordSimilarityResolver(
                     new MistakeRepository<Word>(new Mistake<Word>[]
                     {
                         ("color", "colour", 1)
                     })
                 )
    )
};
```

Also you can add some **character level accepted mistakes**.
In this example we told Simila to not only consider `color` and `colour` similar, but also consider `c` and `k` similar too.

```c#
 var simila = new Simila()
 {
     Resolver = new PhraseSimilarityResolver(
                  new WordSimilarityResolver(
                     new MistakeRepository<Word>(new Mistake<Word>[]
                     {
                         ("color", "colour", 1)
                     }),
                     new CharacterSimilarityResolver(
                        new MistakeRepository<char>(new Mistake<char>[]
                        {
                           ('c', 'k', 1)
                        })
                     )
                 )
    )
};
```
