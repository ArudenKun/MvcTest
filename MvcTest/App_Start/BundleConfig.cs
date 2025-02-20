using System.Web.Optimization;
using BundleTransformer.Core.Bundles;

namespace MvcTest;

public class BundleConfig
{
    // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
    public static void RegisterBundles(BundleCollection bundles)
    {
        bundles.Add(
            new CustomScriptBundle("~/bundles/scripts/preload")
                .Include("~/lib/modernizr/modernizr.js")
                .Include("~/lib/jquery/jquery.js")
                .IncludeDirectory("~/Scripts/PreLoad", "*.js")
        );
        bundles.Add(
            new CustomScriptBundle("~/bundles/scripts")
                .Include("~/lib/bootstrap/js/bootstrap.bundle.js")
                .Include("~/lib/data-tables/datatables.js")
                .Include("~/lib/jquery-validate/jquery.validate.js*")
                .Include("~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js")
                .IncludeDirectory("~/Scripts/PostLoad", "*.js")
        );

        bundles.Add(
            new CustomStyleBundle("~/bundles/css").Include(
                "~/lib/bootstrap/css/bootstrap.css",
                "~/lib/data-tables/datatables.css",
                "~/Content/site.css"
            )
        );
    }
}
