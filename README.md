# OsmApiClient
This is a simple C# client to allow using [OSM API](https://wiki.openstreetmap.org/wiki/API_v0.6) easily.
Please read the API's documentation and use it responsibly.
This project is written in c# and you will need VisualStudio or VS Code to modify it. Pull requests are welcome.

### Features
- Supports Logging using ILogger
- Supports BasicAuth and OAuth
- Supports every operation of the Osm Api v0.6
- Has a [nuget package](https://www.nuget.org/packages/OsmApiClient)
- Is thread safe

# Example Usage
```c#
var clientFactory = new ClientsFactory(null, new HttpClient(), "https://master.apis.dev.openstreetmap.org/api/");
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

See the tests for examples of each operation.

# Supported Operations
### General Api Stuff
- Get Api versions
- Get Api capabilities
- Get a map section
### Change Sets
- Get a specific changeset's metadata
- Get a specific changeset's changes
- Search for changesets
- \*\*Create a new changeset with metadata
- \*\*Add changes to an open changeset
- \*\*Update an open changeset's metadata
- \*\*Close an open changeset
- \*\*Add comments to a changeset
- \*\*Subscribe to a changeset
- \*\*UnSubscribe to a changeset
### Map Elements
- Get an element
- Get an element's version history
- Get a specific version of an element
- Search for elements
- Get relations containing a specific element
- Get ways containing a specific node
- Get a relation and all of its elements
- Get a way and all of its nodes
- \*\*Create a new element
- \*\*Update an element
- \*\*Delete an element
### Gpx Files
- Get trackpoints in an area
- \*Get a gpx file's metadata
- \*Get a gpx file's original upload data
- \*\*Get current user's gpx files
- \*\*Create a new gpx file
- \*\*Update a gpx file's metadata
- \*\*Delete a gpx file
### User Info
- Get details about a user
- Get details about many users
- \*\*Get current user's permissions
- \*\*Get current user's details
- \*\*Get current user's preferences
- \*\*Update current user's preferences
- \*\*Get a current user's preference
- \*\*Update a current user's preference
- \*\*Delete a current user's preference
### Notes
- Search for notes
- Get an RSS feed of notes in an area
- Get a note
- \*Create a new note
- \*Comment on a note
- \*\*Close a note
- \*\*ReOpen a note
\* With or without Authentication

\*\* Requies Authentication