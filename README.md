# FireIceLadder
Terra Mystica Fire &amp; Ice Ladder website

## Notes
- In JSONHelpers/Class1.cs there's a variable called "TopLevelDirectory" which determines where on the server the data and logs get stored. 
- The ConsoleApp project is what actually creates/updates the data (by pulling things from snellman). It just needs to be run with no params. 
- For running the code: the URL is /Home/Ladder
 
## TODO
- [ ] Make "TopLevelDirectory" a config value
- [ ] Change ConsoleApp from a console app to an api call (to be called by a button on the website and/or a regular job)
- [ ] Redirect to /Home/Ladder
