package neoplayer.neoremote;

import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.ImageButton;
import android.widget.ImageView;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;

public class MediaListAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private final ArrayList<MediaData> list;
    private final ArrayList<MediaData> queue;
    private ArrayList<MediaData> filteredList;
    private final ImageButton sortOrder;
    private String filter = "";
    private boolean numSort = false;

    public MediaListAdapter(MainActivity mainActivity, ArrayList<MediaData> list, ArrayList<MediaData> queue) {
        this(mainActivity, list, queue, null);
    }

    public MediaListAdapter(MainActivity mainActivity, ArrayList<MediaData> list, ArrayList<MediaData> queue, ImageButton sortOrder) {
        super();
        this.mainActivity = mainActivity;
        this.list = filteredList = list;
        this.queue = queue;
        this.sortOrder = sortOrder;
        setNumSort(false);
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
                mainActivity.queueVideo(mediaData, false);
            }
        };
        image.setOnClickListener(clickListener);
        view.setOnClickListener(clickListener);

        View.OnLongClickListener onLongClickListener = new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                mainActivity.queueVideo(mediaData, true);
                return true;
            }
        };
        image.setOnLongClickListener(onLongClickListener);
        view.setOnLongClickListener(onLongClickListener);

        return view;
    }

    public void setFilter(String filter) {
        this.filter = filter.toLowerCase();
        notifyDataSetChanged();
    }

    public void toggleNumSort() {
        setNumSort(!numSort);
    }

    public void setNumSort(boolean numSort) {
        this.numSort = numSort;
        if (sortOrder != null)
            sortOrder.setImageResource(numSort ? R.drawable.alphaorder : R.drawable.numorder);
        notifyDataSetChanged();
    }

    @Override
    public void notifyDataSetChanged() {
        filteredList = new ArrayList<>();

        for (MediaData mediaData : list) {
            if ((filter.length() == 0) || (mediaData.description.toLowerCase().contains(filter)))
                filteredList.add(mediaData);
        }

        if (numSort) {
            Collections.sort(filteredList, new Comparator<MediaData>() {
                @Override
                public int compare(MediaData md1, MediaData md2) {
                    return Long.compare(md1.playlistOrder, md2.playlistOrder);
                }
            });
        }

        super.notifyDataSetChanged();
    }
}
