using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;

namespace PanningZoomingApp
{
    [Activity(Label = "PanningZoomingApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        HorizontalScrollView hScrollView;
        CustomLinearLayout customLinearLayout;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            hScrollView = FindViewById<HorizontalScrollView>(Resource.Id.horizontalScrollView);
            customLinearLayout = new PanningZoomingApp.CustomLinearLayout(ApplicationContext);
            hScrollView.AddView(customLinearLayout);
            //TwoDScrollView twoDScrollView = new TwoDScrollView(ApplicationContext);
            //mlinearLayout = FindViewById<LinearLayout>(Resource.Id.parentview);
            //mlinearLayout.AddView(twoDScrollView);
        }

        public override bool DispatchTouchEvent(MotionEvent e)
        {
            customLinearLayout.mScaleGesture.OnTouchEvent(e);
            return base.DispatchTouchEvent(e);
        }
    }
}

