package neoplayer.neoremote;

import android.app.Fragment;
import android.graphics.drawable.Drawable;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageButton;

import com.larvalabs.svgandroid.SVG;
import com.larvalabs.svgandroid.SVGBuilder;

public class VideosFragment extends Fragment {
    public static VideosFragment create() {
        VideosFragment fragment = new VideosFragment();
        return fragment;
    }

    public VideosFragment() {
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
            Bundle savedInstanceState) {
        ViewGroup rootView = (ViewGroup) inflater
                .inflate(R.layout.fragment_videos, container, false);

        SVG svg = new SVGBuilder()
                .readFromResource(getResources(), R.raw.clear) // if svg in res/raw
//                .readFromAsset(getAssets(), "somePicture.svg")           // if svg in assets
                // .setWhiteMode(true) // draw fills in white, doesn't draw strokes
                // .setColorSwap(0xFF008800, 0xFF33AAFF) // swap a single colour
                // .setColorFilter(filter) // run through a colour filter
                // .set[Stroke|Fill]ColorFilter(filter) // apply a colour filter to only the stroke or fill
                .build();

        Log.d("NeoRemote", (svg == null?"NULL":"NON"));
        Drawable drawable = svg.getDrawable();
        ImageButton button = rootView.findViewById(R.id.clearButton);
        button.setImageDrawable(drawable);

        return rootView;
    }
}
