using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Rappen.XTB.Helpers.Extensions
{
    public static class QueryExtensions
    {
        public static bool WarnIfNoIdReturned(this IEnumerable<Entity> result)
        {
            if (result.Any(e => e.Id.Equals(Guid.Empty)))
            {
                MessageBox.Show("There are records without an ID, and those records cannot be updated.\r\n\r\nThis is probably because of two things:\r\nRetrieved with a distinct flag, and the ID attribute is not included in the query.\r\n\r\nPlease fix either issue.", "Missing Id", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return true;
            }
            return false;
        }

        public static bool WarnIf50kReturned(this IEnumerable<Entity> result, string fetch) => WarnIf50kReturned(result, new FetchExpression(fetch));

        public static bool WarnIf50kReturned(this IEnumerable<Entity> result, QueryBase query)
        {
            if (result.Count() == 50000)
            {
                var fxml = XRM.Helpers.FetchXML.Fetch.FromQuery(query);
                var numberorlinkedorders = fxml.Entity?.LinkEntities?.Sum(le => le.Orders?.Count() ?? 0) ?? 0;
                if (numberorlinkedorders > 0)
                {
                    MessageBox.Show($"The query has {numberorlinkedorders} orders in link-entities.\nThis means that max 50000 records will be returned from Dataverse.\n\nClick Help button to read more info about 'Legacy paging' limits at Microsoft Learn.", "Retrieving all pages", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0,
                    "https://learn.microsoft.com/power-apps/developer/data-platform/org-service/paging-behaviors-and-ordering?WT.mc_id=DX-MVP-5002475");
                    return true;
                }
            }
            return false;
        }
    }
}