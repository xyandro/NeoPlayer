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

public class VideoFileListAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private final ArrayList<VideoFile> list;
    private final ArrayList<VideoFile> queue;
    private ArrayList<VideoFile> filteredList;
    private final ImageButton sortOrder;
    private String filter = "";
    private boolean numSort = false;

    public VideoFileListAdapter(MainActivity mainActivity, ArrayList<VideoFile> list, ArrayList<VideoFile> queue) {
        this(mainActivity, list, queue, null);
    }

    public VideoFileListAdapter(MainActivity mainActivity, ArrayList<VideoFile> list, ArrayList<VideoFile> queue, ImageButton sortOrder) {
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
        final VideoFile videoFile = filteredList.get(position);

        boolean found = false;
        for (VideoFile queueVideoFile : queue) {
            if (queueVideoFile.videoFileID == videoFile.videoFileID) {
                found = true;
                break;
            }
        }

        View view = convertView;
        if (view == null)
            view = mainActivity.getLayoutInflater().inflate(R.layout.fragment_media_listitem, parent, false);
        ImageView image = view.findViewById(R.id.image);
        image.setImageResource(found ? R.drawable.check : R.drawable.uncheck);
        ((TextView) view.findViewById(R.id.name)).setText(videoFile.title);
        View.OnClickListener clickListener = new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.queueVideo(videoFile, false);
            }
        };
        image.setOnClickListener(clickListener);
        view.setOnClickListener(clickListener);

        View.OnLongClickListener onLongClickListener = new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                mainActivity.queueVideo(videoFile, true);
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

        for (VideoFile videoFile : list) {
            if ((filter.length() == 0) || (videoFile.title.toLowerCase().contains(filter)))
                filteredList.add(videoFile);
        }

        if (numSort) {
            Collections.sort(filteredList, new Comparator<VideoFile>() {
                @Override
                public int compare(VideoFile md1, VideoFile md2) {
                    return Long.compare(md1.playlistOrder, md2.playlistOrder);
                }
            });
        }

        super.notifyDataSetChanged();
    }
}
