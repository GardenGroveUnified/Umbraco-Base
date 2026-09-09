using WebOptimizer;

namespace UmbracoBase.Core.Bundling
{
    /// <summary>
    /// Registers the single front-end stylesheet bundle served at
    /// <c>/css/site.bundle.css</c>. WebOptimizer combines the files, rewrites
    /// their relative <c>url()</c> paths, minifies (outside Development), and
    /// fingerprints the URL so browsers pick up changes without a hard reload.
    /// </summary>
    public static class StylesheetBundle
    {
        /// <summary>Public route the bundle is served from.</summary>
        public const string Route = "/css/site.bundle.css";

        /// <summary>
        /// Source files, in cascade order. The order is load-bearing: vendor and
        /// theme CSS first, then <c>styles.css</c> (the brand colour custom
        /// properties), then the per-widget CSS that reads those properties.
        /// Add any new widget stylesheet here.
        /// </summary>
        private static readonly string[] SourceFiles =
        [
            "assets/css/vendor.swiper.css",
            "assets/css/core.min.css",
            "assets/css/vendor_bundle.css",
            "assets/css/widget/theme.defaults.css",
            "assets/css/widget/font-awesome.css",
            "assets/css/widget/base.css",
            "assets/css/widget/widgets.css",
            "assets/css/custom.css",
            "css/styles.css",
            "css/mediaQueries.css",
            "css/Widgets_CSS/imageBoxes.css",
            "css/Widgets_CSS/IconBoxes.css",
            "css/Widgets_CSS/Gallery.css",
            "css/Widgets_CSS/Heading.css",
            "css/Widgets_CSS/slider.css",
            "css/Widgets_CSS/text.css",
            "css/Widgets_CSS/Events.css",
            "css/Widgets_CSS/customCode.css",
            "css/Widgets_CSS/Video.css",
            "css/Widgets_CSS/blockLine.css",
            "css/Widgets_CSS/Hero.css",
            "css/Widgets_CSS/Staff.css",
            "css/Widgets_CSS/Accordion.css",
            "css/Widgets_CSS/Tab.css",
            "css/Widgets_CSS/Notification.css",
            "css/Widgets_CSS/Modal.css",
            "css/Widgets_CSS/Home.css",
            "css/Widgets_CSS/Athletics.css",
            "css/Widgets_CSS/Guidance.css",
            "css/Widgets_CSS/Sidebar.css",
            "css/Widgets_CSS/cardBox.css",
            "css/Widgets_CSS/staffDirectory.css",
            "css/Widgets_CSS/studentLinks.css",
            "css/Widgets_CSS/clubsGrid.css",
            "css/Widgets_CSS/noticeBanner.css",
            "css/Widgets_CSS/newsFeed.css",
            "css/Widgets_CSS/achievements.css",
            "assets/css/vendor.fancybox.min.css",
        ];

        public static IServiceCollection AddSiteStylesheetBundle(
            this IServiceCollection services, IWebHostEnvironment environment)
        {
            services.AddWebOptimizer(pipeline =>
            {
                if (environment.IsDevelopment())
                {
                    // Concatenate only. Unminified output stays readable in
                    // dev tools, and WebOptimizer re-reads a source file when
                    // it changes.
                    pipeline.AddBundle(Route, "text/css; charset=UTF-8", SourceFiles)
                            .AdjustRelativePaths()
                            .Concatenate();
                }
                else
                {
                    pipeline.AddCssBundle(Route, SourceFiles);
                }
            });

            return services;
        }
    }
}
