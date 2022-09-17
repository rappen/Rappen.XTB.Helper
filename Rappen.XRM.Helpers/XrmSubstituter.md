# XrmSubstituter

Code implements the `XRM Tokens` feature.

Check all code right here:
[XrmSubstituter.cs](https://github.com/rappen/Rappen.XTB.Helper/blob/main/Rappen.XRM.Helpers/XrmSubstituter.cs)

Easiest way to use it:
`public static string Substitute(this Entity entity, IOrganizationService service, string text)`

Example:
`var theText = myEntity.Substitute(service, "Name is {name} from {address1_city}, owned by {ownerid.firstname} with id {ownerid|<value>}.");`

See all documents at https://jonasr.app/xrm-tokens/
