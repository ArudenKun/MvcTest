using System.Web.Optimization;
using BundleTransformer.Core.Bundles;
using BundleTransformer.Core.Orderers;

namespace MvcTest;

public class BundleConfig
{
    // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
    public static void RegisterBundles(BundleCollection bundles)
    {
        bundles.Add(
            new CustomScriptBundle("~/bundles/scripts/preload") { Orderer = new NullOrderer() }
                .Include("~/lib/modernizr/modernizr.js")
                .Include("~/lib/jquery/jquery.js")
                .Include("~/lib/filepond/dist/filepond.js")
                .Include("~/lib/filepond/filepond.jquery.js")
                .IncludeDirectory("~/Scripts/PreLoad/", "*.js")
        );
        bundles.Add(
            new CustomScriptBundle("~/bundles/scripts") { Orderer = new NullOrderer() }
                .Include("~/lib/bootstrap/js/bootstrap.bundle.js")
                .Include("~/lib/data-tables/datatables.js")
                .Include("~/lib/jquery-validate/jquery.validate.js*")
                .Include("~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js")
                .Include("~/lib/sweetalert2/sweetalert2.js")
                .IncludeDirectory("~/lib/filepond/dist/", "filepond-plugin-*", true)
                .IncludeDirectory("~/Scripts/PostLoad", "*.js")
        );

        bundles.Add(
            new CustomStyleBundle("~/bundles/css").Include(
                "~/lib/bootstrap/css/bootstrap.css",
                "~/lib/data-tables/datatables.css",
                "~/lib/filepond/dist/filepond.css",
                "~/lib/sweetalert2/sweetalert2.css",
                "~/Content/site.css"
            )
        );

#if DEBUG
        BundleTable.EnableOptimizations = false;
#else
        BundleTable.EnableOptimizations = true;
#endif
    }
}
