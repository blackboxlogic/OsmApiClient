# OsmApiClient

This is a simple C# client to allow using [OSM API](https://wiki.openstreetmap.org/wiki/API_v0.6) easily.

To work with this project, you will need VisualStudio or VS Code.
Pull requests are welcome.

------------------------
# Supported Opperations
\* With or without Authentication
\*\* Requies Authentication
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