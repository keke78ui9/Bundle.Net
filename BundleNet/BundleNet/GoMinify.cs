using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Optimization;


//http://weblogs.asp.net/imranbaloch/bundling-and-minifying-inline-css-and-js
//http://ajaxmin.codeplex.com/SourceControl/latest
namespace BundleNet
{
    // TODO: extension could to the bundle.setname(0.
    /// <summary>
    /// TODO-LiST
    /// * issue when reference site is using version less than Pack project's dll version. like System.Web.Optimization(1.1.0) but Pack is using (1.1.3)
    /// bundle css/js
    /// 1. done - bundle global
    /// 2. done - bundle for individual pages css/js file
    /// 3. not done - bundle for individual pages' css/js section
    /// 4. not done - configuration for bundle
    /// 5. not done - bundle *.eot, *ttf, *woff files.
    /// 6. not done - Content or Scripts not accessable.
    /// </summary>
    public class GoMinify
    {


        private const string REGEX_SCRIPT_TAG = "<script.*?>.*?</script>";
        private const string REGEX_STYLE_TAG = "<style.*?>.*?</style>";
        private const string PREFIX_BUNDLE_TILDE_NAME = "~/";

        /// <summary>
        /// enable bundle depend on the config || request param
        /// </summary>
        public static bool EnableBundle
        {
            get
            {
                if (string.IsNullOrWhiteSpace(HttpContext.Current.Request["ebj"] + string.Empty))
                {
                    return true;
                }

                return false;
            }
        }

        //public static IHtmlString JsFiles(object parameters)
        //{
        //    return BaseBundle(parameters.BundleType, parameters.Name, parameters.files);
        //}

        public static IHtmlString JsGlobal(params string[] filePaths)
        {
            return BaseBundle(MinifyType.Javascript, "global", filePaths);
        }

        public static IHtmlString Js(params string[] filePaths)
        {
            return BaseBundle(MinifyType.Javascript, string.Empty, filePaths);
        }
        public static IHtmlString CssGlobal(params string[] filePaths)
        {
            return BaseBundle(MinifyType.Css, "global", filePaths);
        }

        public static IHtmlString Css(params string[] filePaths)
        {
            return BaseBundle(MinifyType.Css, string.Empty, filePaths);
        }

        public static string JsInline(Func<object, object> textFunc)
        {
            return BundleInlines(MinifyType.Javascript, textFunc);
        }

        public static string CssInline(Func<object, object> textFunc)
        {
            return BundleInlines(MinifyType.Css, textFunc);
        }

        /// <summary>
        /// TODO: make it more flexible
        /// </summary>
        public static string BundleName
        {
            get
            {
                return PREFIX_BUNDLE_TILDE_NAME + GetUniqueName();
            }
        }


        internal static IHtmlString BaseBundle(MinifyType bundleType, string bundleName, params string[] filePaths)
        {
            var newFiles = CheckFiles(filePaths);
            if (newFiles == null)
            {
                return new HtmlString(string.Empty);
            }

            var bundles = BundleTable.Bundles;
            BundleTable.EnableOptimizations = EnableBundle;

            string name = "";
            if (string.IsNullOrWhiteSpace(bundleName))
            {
                name = BundleName;
            }

            IHtmlString htmlString = null;

            switch (bundleType)
            {
                case MinifyType.Javascript:
                    //var bundle = new ScriptBundle(name);
                    //bundle.Transforms.Add(new MyBundlePlatform("BEGIN TEST INTERFACE", "END TEST INTERFACE"));
                    //bundle.Include(newFiles);
                    var bundle = new Bundle(name, new MyBundlePlatform("// BEGIN TEST INTERFACE", "// END TEST INTERFACE"));
                    bundle.Include(newFiles);
                    bundles.Add(bundle);
                    return Scripts.Render(name);
                case MinifyType.Css:
                    bundles.Add(new StyleBundle(name).Include(newFiles));
                    return Styles.Render(name);
            }

            return new HtmlString(string.Empty);
        }

        private static string BundleInlines(MinifyType bundleType, Func<object, object> textFunc)
        {
            string notMinified = string.Empty;
            notMinified = (textFunc.Invoke(new object()) ?? "").ToString();

            if (!EnableBundle)
            {
                return notMinified;
            }

            var minifier = new Minifier();
            string minified = string.Empty;

            minifier.MinifyJavaScript("", new CodeSettings());

            switch (bundleType)
            {
                case MinifyType.Javascript:
                    minified = minifier.MinifyJavaScript(notMinified, new CodeSettings
                    {
                        EvalTreatment = EvalTreatment.MakeImmediateSafe,
                        PreserveImportantComments = false
                    });
                    break;
                case MinifyType.Css:
                    minified = minifier.MinifyStyleSheet(notMinified, new CssSettings
                    {
                    });
                    break;
                default:
                    break;
            }

            return minified;
        }


        internal static string[] CheckFiles(string[] filePaths)
        {
            if (filePaths == null || filePaths.Count() < 1)
            {
                return null;
            }
            List<string> newFiles = new List<string>();
            filePaths.ToList().ForEach(file =>
            {
                // becasue BundleTable's add new Bundle logic require relative URLs (~/url)
                if (file[0] != '~')
                {
                    file = '~' + file;
                }

                newFiles.Add(file);
            });

            return newFiles.ToArray();
        }
        internal static string GetUniqueName()
        {
            Guid guid = Guid.NewGuid();
            return Convert.ToBase64String(guid.ToByteArray()).Replace("=", string.Empty).Replace("+", string.Empty).Replace("/", string.Empty);
        }

        private static string TryGetAppSetting(string key, string defaultValue)
        {
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings.Get(key)))
            {
                return ConfigurationManager.AppSettings[key];
            }

            return defaultValue;
        }
    }
}
