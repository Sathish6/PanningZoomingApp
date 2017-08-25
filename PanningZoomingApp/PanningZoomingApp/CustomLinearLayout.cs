using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace PanningZoomingApp
{
    internal class CustomLinearLayout : LinearLayout,ScaleGestureDetector.IOnScaleGestureListener
    {
        internal ScaleGestureDetector mScaleGesture;
        internal float mScaleFactor = 1;
        internal float mPosX;
        internal float mPosY;

        internal float mLastTouchX;
        internal float mLastTouchY;

        internal float mFocusY;
        internal float mFocusX;
        private const int INVALID_POINTER_ID = 1;
        private int mActivePointerId = INVALID_POINTER_ID;
        private bool m_isScaling = false;
        private Matrix mTranslateMatrix = new Matrix();
        private Matrix mTranslateMatrixInverse = new Matrix();

        private Matrix mScaleMatrix = new Matrix();
        private Matrix mScaleMatrixInverse = new Matrix();

        private int mCanvasWidth;
        private int mCanvasHeight;
        List<int> imageList = new List<int> { Resource.Drawable.image1, Resource.Drawable.image2, Resource.Drawable.image3, Resource.Drawable.image4 };
        internal CustomLinearLayout(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            this.Orientation = Orientation.Vertical;
            mScaleGesture = new ScaleGestureDetector(Context, this);
            InitializeLayout();
        }
        internal CustomLinearLayout(Context context) : base(context)
        {
            this.Orientation = Orientation.Vertical;
            mScaleGesture = new ScaleGestureDetector(Context, this);
            InitializeLayout();
        }

        private Size CalculateOptimalSizeOfImage(float width, float height)
        {
            float maxWidth = this.Width, maxHeight = this.Height;
            float w = width, h = height;
            float ratio = w / h;
            w = maxWidth;
            h = (float)Java.Lang.Math.Floor(maxWidth / ratio);
            if (h > maxHeight)
            {
                h = maxHeight;
                w = (float)Java.Lang.Math.Floor(maxHeight * ratio);
            }
            float multiplicationFactorWidth = ((float)this.Width / w);
            return new Size((int)(w * multiplicationFactorWidth), (int)(h * multiplicationFactorWidth));
        }

        private void InitializeLayout()
        {
            for (int i=0;i<imageList.Count;i++)
            {
                Bitmap bitmap = BitmapFactory.DecodeResource(Context.Resources, imageList[i]);
                Size optimalSize = CalculateOptimalSizeOfImage(bitmap.Width, bitmap.Height);
                LinearLayout.LayoutParams layoutParams = new LayoutParams((int)optimalSize.Width, (int)optimalSize.Height);
                layoutParams.SetMargins(0, 2, 0, 2);
                layoutParams.Gravity = GravityFlags.Center;
                ImageView imgView = new ImageView(Context);
                imgView.SetImageBitmap(bitmap);
                this.AddView(imgView, i);
            }
            LinearLayout.LayoutParams linearlayoutParams = new LinearLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
            linearlayoutParams.Gravity = GravityFlags.Center;
            this.LayoutParameters = linearlayoutParams;
        }

        public bool OnScale(ScaleGestureDetector detector)
        {
            m_isScaling = true;
            mScaleFactor *= detector.ScaleFactor;
            if (detector.IsInProgress)
            {
                mFocusX = detector.FocusX;
                mFocusY = detector.FocusY;
            }


            mFocusX = (mFocusX + mLastTouchX) / 2;  // get center of touch
            mFocusY = (mFocusY + mLastTouchY) / 2;  // get center of touch

            mScaleFactor = Math.Max(1f, Math.Min(mScaleFactor, 3.0f));
            Invalidate();
            return true;
        }

        protected override void DispatchDraw(Canvas canvas)
        {
            canvas.Save();
            canvas.Scale(mScaleFactor, mScaleFactor, mFocusX, mFocusY);
            canvas.Translate(mPosX/mScaleFactor, mPosY/mScaleFactor);
            base.DispatchDraw(canvas);
            canvas.Restore();
        }
        public bool OnScaleBegin(ScaleGestureDetector detector)
        {
            m_isScaling = true;
            return true;
        }

        public void OnScaleEnd(ScaleGestureDetector detector)
        {
            m_isScaling = false;
        }

        private float[] screenPointsToScaledPoints(float[] a)
        {
            mTranslateMatrixInverse.MapPoints(a);
            mScaleMatrixInverse.MapPoints(a);
            return a;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            //int originalWidth = MeasureSpec.GetSize(widthMeasureSpec);
            //int originalHeight = MeasureSpec.GetSize(heightMeasureSpec);
            //int scaledWidth = (int)(originalWidth * mScaleFactor);
            //int scaledHeight = (int)(originalHeight * mScaleFactor);
            //SetMeasuredDimension(Math.Min(originalWidth, scaledWidth), Math.Min(originalHeight, scaledHeight));
            int height = 0;
            int width = 0;
            int childCount = ChildCount;
            for (int i = 0; i < childCount; i++)
            {
                View child = GetChildAt(i);
                if (child.Visibility != ViewStates.Gone)
                {
                    MeasureChild(child, widthMeasureSpec, heightMeasureSpec);
                    height += child.MeasuredHeight;
                    width = Math.Max(width, child.MeasuredWidth);
                }
            }
            mCanvasWidth = width;
            mCanvasHeight = height;
        }
        public override bool OnTouchEvent(MotionEvent ev)
        {

            int action = ev.ActionIndex;
            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    {
                        float x = ev.GetX();
                        float y = ev.GetY();

                        mLastTouchX = x;
                        mLastTouchY = y;

                        // Save the ID of this pointer
                        mActivePointerId = ev.GetPointerId(0);
                        break;
                    }

                case MotionEventActions.Move:
                    {
                        // Find the index of the active pointer and fetch its position
                        int pointerIndex = ev.FindPointerIndex(mActivePointerId);
                        float x = ev.GetX(pointerIndex);
                        float y = ev.GetY(pointerIndex);

                        if (m_isScaling && ev.PointerCount == 1)
                        {
                            // Don't move during a QuickScale.
                            mLastTouchX = x;
                            mLastTouchY = y;

                            break;
                        }

                        float dx = x - mLastTouchX;
                        float dy = y - mLastTouchY;

                        float[] topLeft = { 0f, 0f };
                        float[] bottomRight = { Width, Height };
                        /*
                         * Corners of the view in screen coordinates, so dx/dy should not be allowed to
                         * push these beyond the canvas bounds.
                         */
                        float[] scaledTopLeft = screenPointsToScaledPoints(topLeft);
                        float[] scaledBottomRight = screenPointsToScaledPoints(bottomRight);

                        dx = Math.Min(Math.Max(dx, scaledBottomRight[0] - mCanvasWidth), scaledTopLeft[0]);
                        dy = Math.Min(Math.Max(dy, scaledBottomRight[1] - mCanvasHeight), scaledTopLeft[1]);

                        mPosX += dx;
                        mPosY += dy;

                        mTranslateMatrix.PreTranslate(dx, dy);
                        mTranslateMatrix.Invert(mTranslateMatrixInverse);

                        mLastTouchX = x;
                        mLastTouchY = y;

                        Invalidate();
                        break;
                    }

                case MotionEventActions.Up:
                    {
                        mActivePointerId = INVALID_POINTER_ID;
                        break;
                    }

                case MotionEventActions.Cancel:
                    {
                        mActivePointerId = INVALID_POINTER_ID;
                        break;
                    }

                case MotionEventActions.Pointer1Up:
                    {
                        // Extract the index of the pointer that left the touch sensor
                        int pointerIndex = ev.ActionIndex;
                        int pointerId = ev.GetPointerId(pointerIndex);
                        if (pointerId == mActivePointerId)
                        {
                            // This was our active pointer going up. Choose a new
                            // active pointer and adjust accordingly.
                            int newPointerIndex = pointerIndex == 0 ? 1 : 0;
                            mLastTouchX = ev.GetX(newPointerIndex);
                            mLastTouchY = ev.GetY(newPointerIndex);
                            mActivePointerId = ev.GetPointerId(newPointerIndex);
                        }
                        break;
                    }
            }
            Invalidate();
            return true;
        }
    }
}
