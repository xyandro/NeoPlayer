package neoplayer.neoremote;

import android.support.v4.view.PagerAdapter;
import android.support.v4.view.ViewPager;
import android.view.View;
import android.view.ViewGroup;

class ScreenSlidePagerAdapter extends PagerAdapter {
    final int pageCount;

    public ScreenSlidePagerAdapter(ViewPager pager) {
        pageCount = pager.getChildCount();
        pager.setOffscreenPageLimit(pageCount);
        pager.setAdapter(this);
        pager.setPageTransformer(true, new ZoomOutPageTransformer());
    }

    @Override
    public Object instantiateItem(ViewGroup collection, int position) {
        return collection.getChildAt(position);
    }

    @Override
    public int getCount() {
        return pageCount;
    }

    @Override
    public boolean isViewFromObject(View arg0, Object arg1) {
        return arg0 == arg1;
    }
}
