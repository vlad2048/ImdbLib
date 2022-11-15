# Intro

Library and LINQPad scripts to gather, store and update data about movies in IMDB

&nbsp;

# Usage
```c#
using var imdb = new ImdbScraper(opt =>
{
  // Setup options
  opt.DataFolder = @"C:\IMDBData";

  // Set this to true for debugging
  // this will make the scraper work off a much smaller dataset
  opt.DbgUseSmallDatasets = true;
});
await imdb.Init();
imdb.Start();
imdb.Stop();
```
Run ```scrape.linq``` in LINQPad to run the scraping

&nbsp;

# Data flow and options

## Init()

### Step 1: Get IMDB dataset
```datasets\title.basics.tsv```
This dataset is provided by IMDB and represents basic info about all the titles.
If it's not present or if our copy is older than ```opt.DatasetRefreshPeriod``` (default 60 days) we download it.

### Step 2: Extract dev dataset
```datasets-dev\title.basics.tsv```
If you set ```opt.DbgUseSmallDatasets```, it will extract a much smaller dataset (1000 titles) to use in the rest of the data flow.

### Step 3: Compile titles
```datasets\titles.json```
(or ```datasets-dev\titles.json``` if ```opt.DbgUseSmallDatasets``` is set)
- Applies a filter specified in ```opt.TitleFilter``` to restrict the movies we should consider
- Compiles the cleaned up results in this file

This file is regenerated if either:
- it doesn't exist
- the IMDB dataset was refreshed
- the user sets ```opt.RefreshTitleFilter```

The default filter is:
```c#
opt.TitleFilter = e =>
  !e.IsAdult &&
  e.Type == "movie" &&
  e.StartYear is >= 1970;
```
**Note:** the filter needs to discard the titles with a null StartYear for the rest of the data flow to work correctly.

These titles represent the list of all the movies we want to scrape

&nbsp;

## Start()
Now we have the list of titles we're interested in, we will scrape the info for them.
```Start()``` kicks off the process and is controlled by these options:
- ```opt.ScrapeBatchSize``` the number of movies to scrape before saving (default 64)
- ```opt.ScrapeParallelism``` determines how many movies we scrape in parallel (default 4)

There is a file to keep track of the state of each movie:
```scraping\title-states.json```

And the movies are grouped by year in files named like this:
```scraping\1988.json```

Note: if using ```opt.DbgUseSmallDatasets``` the files will be in the ```scraping-dev\``` folder.

If we detect that IMDB rate limits us, we will pause for 10min.

&nbsp;

# LINQPad scripts

## ```scrape.linq```
Run the scraper

## ```scrape-dbg.linq```
Used to debug / tweak the scraping.
It has an exact copy of the HtmlScraper class.
The different functions are well documented in the script.

## ```finder.linq```
Used to query the films and nicely display the results.

Use this to find good movies!
