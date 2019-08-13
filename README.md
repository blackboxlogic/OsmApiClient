# OsmApiClient

This is a simple C# client to allow using [OSM API](https://wiki.openstreetmap.org/wiki/API_v0.6) easily.

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
- [ ] POST /api/0.6/changeset/#id/expand_bbox
- [ ] GET /api/0.6/changesets
- [x] POST /api/0.6/changeset/#id/upload
- [ ] POST /api/0.6/changeset/#id/comment
- [ ] POST /api/0.6/changeset/#id/subscribe
- [ ] POST /api/0.6/changeset/#id/unsubscribe
### Elements
- [ ] PUT /api/0.6/[node|way|relation]/create
- [x] GET /api/0.6/[node|way|relation]/#id
- [ ] PUT /api/0.6/[node|way|relation]/#id
- [ ] DELETE /api/0.6/[node|way|relation]/#id
- [ ] GET /api/0.6/[node|way|relation]/#id/history
- [ ] GET /api/0.6/[node|way|relation]/#id/#version
- [ ] GET /api/0.6/[nodes|ways|relations]?#parameters
- [ ] GET /api/0.6/[node|way|relation]
- [ ] POST /api/0.6/[node|way|relation]/#id/#version/redact?redaction=#redaction_id
### Gpx Files
- [ ] GET /api/0.6/trackpoints?bbox=left,bottom,right,top&page=pageNumber
- [ ] POST /api/0.6/gpx/create
- [ ] GET /api/0.6/gpx//details
- [ ] GET /api/0.6/gpx//data
### User Info
- [ ] GET /api/0.6/user/gpx_files
- [ ] GET /api/0.6/user/#id
- [ ] GET /api/0.6/users?users=#id1,#id2,...,#idn
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