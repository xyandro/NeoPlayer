package neoplayer.neoremote;

import android.app.Fragment;
import android.content.Context;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.ImageView;
import android.widget.ListView;
import android.widget.TextView;

import java.util.ArrayList;

public class VideosFragment extends Fragment {
    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        View result = inflater.inflate(R.layout.fragment_videos, container, false);

        ListView listView = result.findViewById(R.id.videosList);
        ArrayList<VideoData> listItems = new ArrayList<VideoData>();

        listItems.add(new VideoData("Randon Spackman", "Randon", false));
        listItems.add(new VideoData("Ben Christensen", "Ben", true));
        listItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        listItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        listItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        listItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        listItems.add(new VideoData("Megan Spackman", "Megan", true));
        listItems.add(new VideoData("Randon Spackman", "Randon", false));
        listItems.add(new VideoData("Ben Christensen", "Ben", true));
        listItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        listItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        listItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        listItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        listItems.add(new VideoData("Megan Spackman", "Megan", true));
        listItems.add(new VideoData("Randon Spackman", "Randon", false));
        listItems.add(new VideoData("Ben Christensen", "Ben", true));
        listItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        listItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        listItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        listItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        listItems.add(new VideoData("Megan Spackman", "Megan", true));
        listItems.add(new VideoData("Randon Spackman", "Randon", false));
        listItems.add(new VideoData("Ben Christensen", "Ben", true));
        listItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        listItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        listItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        listItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        listItems.add(new VideoData("Megan Spackman", "Megan", true));
        listItems.add(new VideoData("Randon Spackman", "Randon", false));
        listItems.add(new VideoData("Ben Christensen", "Ben", true));
        listItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        listItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        listItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        listItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        listItems.add(new VideoData("Megan Spackman", "Megan", true));

        final VideosListAdapter adapter = new VideosListAdapter(getContext(), listItems);
        listView.setAdapter(adapter);

        return result;
    }

    private class VideosListAdapter extends ArrayAdapter<VideoData> {
        private final Context context;
        private final ArrayList<VideoData> list;

        public VideosListAdapter(Context context, ArrayList<VideoData> list) {
            super(context, -1, list);
            this.context = context;
            this.list = list;
        }

        @Override
        public View getView(int position, View convertView, ViewGroup parent) {
            LayoutInflater inflater = (LayoutInflater) context.getSystemService(Context.LAYOUT_INFLATER_SERVICE);
            View rowView = inflater.inflate(R.layout.fragment_videos_listitem, parent, false);

            VideoData videoData = list.get(position);

            ImageView imageView = rowView.findViewById(R.id.image);
            imageView.setImageResource(videoData.Selected ? R.drawable.check : R.drawable.uncheck);

            TextView textView = rowView.findViewById(R.id.name);
            textView.setText(videoData.Description);

            return rowView;
        }
    }
}
