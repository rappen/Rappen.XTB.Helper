# Rappen.XRM.Tokens extensions shared project

Code implements the `XRM Tokens` feature.

Easiest way to use it:
`public static string Tokens(this Entity entity, IOrganizationService service, string text)`

Example:
`var theText = myEntity.Tokens(service, "Name is {name} from {address1_city}, owned by {ownerid.firstname} with id {ownerid|<value>}.");`

See all documents at https://jonasr.app/xrm-tokens/
