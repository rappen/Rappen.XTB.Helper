# XrmSubstituter

Code implement the `XRM Tokens`.

Check all code!

Easiest way to use it:
`public static string Substitute(this Entity entity, IOrganizationService service, string text)`

Example:
`var theText = myEntity.Substitute(service, "Name is {name} from {address1_city}, owned by {ownerid.firstname} with id {ownerid|<value>}.");`

See all documents at https://jonasr.app/xrm-tokens/
