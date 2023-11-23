# DEPRECATED
Development has moved to https://github.com/OsmSharp/io-api

---

# OsmApiClient
This is a simple C# client to allow using [OSM API](https://wiki.openstreetmap.org/wiki/API_v0.6) easily.
Please read the API's documentation and use it responsibly. Misuse can have an adverse affect on the OSM ecosystem.
Pull requests are welcome. You will need VisualStudio or VS Code to modify this project.

### Features
- Supports Logging using ILogger
- Supports BasicAuth, OAuth 1 and OAuth 2
- Supports every documented operation of the Osm Api v0.6
- Has a [nuget package](https://www.nuget.org/packages/OsmApiClient)
- Is thread safe

# Example Usage
```c#
using OsmSharp.IO.API;
// Create a client factory (pointing at the dev server)
var clientFactory = new ClientsFactory(null, new HttpClient(),
	"https://master.apis.dev.openstreetmap.org/api/");
// After testing, use "https://www.openstreetmap.org/api/" for production
```

### Get a Node
```c#
var client = clientFactory.CreateNonAuthClient();
var node = await client.GetNode(100);
```

### Delete a Node (map changes require BasicAuth or OAuth)
```c#
var authClient = clientFactory.CreateBasicAuthClient("username", "password");
var changeSetTags = new TagsCollection() { new Tag("comment", "Deleting a node.") };
var changeSetId = await client.CreateChangeset(changeSetTags);
node.Version = await client.DeleteElement(changeSetId, node);
await client.CloseChangeset(changeSetId);
```

See the [functional tests](https://github.com/blackboxlogic/OsmApiClient/tree/master/OsmSharp.IO.API.Tests) for examples of each operation.

# Supported Operations

:full_moon: = Does not use authentication

:last_quarter_moon: = With or without authentication (behavior may differ)

:new_moon: = Requires authentication

### General Api Stuff
- :full_moon: Get Api versions
- :full_moon: Get Api capabilities
- :full_moon: Get a map section
### Change Sets
- :full_moon: Get a specific changeset's metadata
- :full_moon: Get a specific changeset's changes
- :full_moon: Search for changesets
- :new_moon: Create a new changeset with metadata
- :new_moon: Add changes to an open changeset
- :new_moon: Update an open changeset's metadata
- :new_moon: Close an open changeset
- :new_moon: Add comments to a changeset
- :new_moon: Subscribe to a changeset
- :new_moon: UnSubscribe to a changeset
### Map Elements
- :full_moon: Get an element
- :full_moon: Get an element's version history
- :full_moon: Get a specific version of an element
- :full_moon: Search for elements
- :full_moon: Get relations containing a specific element
- :full_moon: Get ways containing a specific node
- :full_moon: Get a relation and all of its elements
- :full_moon: Get a way and all of its nodes
- :new_moon: Create a new element
- :new_moon: Update an element
- :new_moon: Delete an element
### Gpx Files
- :full_moon: Get trackpoints in an area
- :last_quarter_moon: Get a gpx file's metadata
- :last_quarter_moon: Get a gpx file's original upload data
- :new_moon: Get current user's gpx files
- :new_moon: Create a new gpx file
- :new_moon: Update a gpx file's metadata
- :new_moon: Delete a gpx file
### User Info
- :full_moon: Get details about a user
- :full_moon: Get details about many users
- :new_moon: Get current user's permissions
- :new_moon: Get current user's details
- :new_moon: Get current user's preferences
- :new_moon: Update current user's preferences
- :new_moon: Get a current user's preference
- :new_moon: Update a current user's preference
- :new_moon: Delete a current user's preference
### Notes
- :full_moon: Search for notes
- :full_moon: Get an RSS feed of notes in an area
- :full_moon: Get a note
- :last_quarter_moon: Create a new note
- :new_moon: Comment on a note
- :new_moon: Close a note
- :new_moon: ReOpen a note

# Contribute
Issues and pull requests are welcome.
