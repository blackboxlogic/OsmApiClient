# OsmApiClient

This is a simple C# client to allow using [OSM API](https://wiki.openstreetmap.org/wiki/API_v0.6) easily.

To work with this project, you will need VisualStudio or VS Code.
Pull requests are welcome.

# Supported Opperations:
- [x] GET /api/versions
- [x] GET /api/capabilities
- [x] GET /api/0.6/map
- [x] GET /api/0.6/permissions
### Change Sets
- [x] PUT /api/0.6/changeset/create
- [x] GET /api/0.6/changeset/#id?include_discussion=true
- [x] PUT /api/0.6/changeset/#id
- [x] PUT /api/0.6/changeset/#id/close
- [x] GET /api/0.6/changeset/#id/download
- [x] GET /api/0.6/changesets
- [x] POST /api/0.6/changeset/#id/upload
### Change Set Discussion
- [x] POST /api/0.6/changeset/#id/comment
- [x] POST /api/0.6/changeset/#id/subscribe
- [x] POST /api/0.6/changeset/#id/unsubscribe
### Elements
- [x] PUT /api/0.6/[node|way|relation]/create
- [x] GET /api/0.6/[node|way|relation]/#id
- [x] PUT /api/0.6/[node|way|relation]/#id
- [x] DELETE /api/0.6/[node|way|relation]/#id
- [x] GET /api/0.6/[node|way|relation]/#id/history
- [x] GET /api/0.6/[node|way|relation]/#id/#version
- [x] GET /api/0.6/[nodes|ways|relations]?#parameters
- [x] GET /api/0.6/[node|way|relation]/#id/relations
- [x] GET /api/0.6/node/#id/ways
- [x] GET /api/0.6/[way|relation]/#id/full
### Gpx Files
- [x] GET /api/0.6/trackpoints?bbox=left,bottom,right,top&page=pageNumber
- [x] GET /api/0.6/user/gpx_files
- [x] GET /api/0.6/gpx/#id/details
- [x] GET /api/0.6/gpx/#id/data
- [x] POST /api/0.6/gpx/create
- [x] PUT /api/0.6/gpx/#id
- [x] DELETE /api/0.6/gpx/#id
### User Info
- [x] GET /api/0.6/user/#id
- [x] GET /api/0.6/users?users=#id1,#id2,...,#idn
- [x] GET /api/0.6/user/details
- [ ] GET /api/0.6/user/preferences
- [ ] GET /api/0.6/user/preferences/[your_key]
- [ ] PUT /api/0.6/user/preferences/[your_key]
- [ ] DELETE /api/0.6/user/preferences/[your_key]
### Notes
- [ ] GET /api/0.6/notes
- [ ] GET /api/0.6/notes?bbox=left,bottom,right,top
- [ ] GET /api/0.6/notes/#id
- [ ] POST /api/0.6/notes
- [ ] POST /api/0.6/notes/#id/comment
- [ ] POST /api/0.6/notes/#id/close
- [ ] POST /api/0.6/notes/#id/reopen
- [ ] GET /api/0.6/notes/search