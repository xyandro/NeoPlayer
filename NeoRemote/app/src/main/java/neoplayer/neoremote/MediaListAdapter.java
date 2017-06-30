package neoplayer.neoremote;

import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.ImageView;
import android.widget.TextView;

import java.util.ArrayList;

public class MediaListAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private final ArrayList<MediaData> list;
    private final ArrayList<MediaData> queue;
    private ArrayList<MediaData> filteredList;
    private String filter = "";

    public MediaListAdapter(MainActivity mainActivity, ArrayList<MediaData> list, ArrayList<MediaData> queue) {
        super();
        this.mainActivity = mainActivity;
        this.list = filteredList = list;
        this.queue = queue;
    }

    @Override
    public int getCount() {
        return filteredList.size();
    }

    @Override
    public Object getItem(int i) {
        return filteredList.get(i);
    }

    @Override
    public long getItemId(int i) {
        return i;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        final MediaData mediaData = filteredList.get(position);

        boolean found = false;
        for (MediaData queueMediaData : queue) {
            if (queueMediaData.url.equals(mediaData.url)) {
                found = true;
                break;
            }
        }

        View view = convertView;
        if (view == null)
            view = mainActivity.getLayoutInflater().inflate(R.layout.fragment_media_listitem, parent, false);
        ImageView image = view.findViewById(R.id.image);
        image.setImageResource(found ? R.drawable.check : R.drawable.uncheck);
        ((TextView) view.findViewById(R.id.name)).setText(mediaData.description);
        View.OnClickListener clickListener = new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.queueVideo(mediaData);
            }
        };
        image.setOnClickListener(clickListener);
        view.setOnClickListener(clickListener);

        return view;
    }

    public void setFilter(String filter) {
        this.filter = filter.toLowerCase();
        notifyDataSetChanged();
    }

    @Override
    public void notifyDataSetChanged() {
        if (filter.length() == 0) {
            filteredList = list;
        } else {
            filteredList = new ArrayList<>();
            for (MediaData mediaData : list) {
                if (mediaData.description.toLowerCase().contains(filter))
                    filteredList.add(mediaData);
            }
        }

        super.notifyDataSetChanged();
    }
}
