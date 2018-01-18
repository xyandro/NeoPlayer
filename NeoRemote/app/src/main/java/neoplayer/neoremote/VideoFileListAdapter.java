package neoplayer.neoremote;

import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.ImageView;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;

public class VideoFileListAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private HashMap<Integer, VideoFile> videoFiles = new HashMap<>();
    private HashSet<Integer> checkIDs = new HashSet<>();
    private ArrayList<Integer> showIDs = new ArrayList<>();
    private final ArrayList<VideoFile> displayList = new ArrayList<>();

    public VideoFileListAdapter(MainActivity mainActivity) {
        super();
        this.mainActivity = mainActivity;
    }

    public void setVideoFiles(HashMap<Integer, VideoFile> videoFiles) {
        this.videoFiles = videoFiles;
        notifyDataSetChanged();
    }

    public void setCheckIDs(HashSet<Integer> checkIDs) {
        this.checkIDs = checkIDs;
        notifyDataSetChanged();
    }

    public void setShowIDs(ArrayList<Integer> showIDs) {
        this.showIDs = showIDs;
        notifyDataSetChanged();
    }

    @Override
    public int getCount() {
        return displayList.size();
    }

    @Override
    public Object getItem(int i) {
        return displayList.get(i);
    }

    @Override
    public long getItemId(int i) {
        return i;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        final VideoFile videoFile = displayList.get(position);

        View view = convertView;
        if (view == null)
            view = mainActivity.getLayoutInflater().inflate(R.layout.fragment_media_listitem, parent, false);
        ImageView image = view.findViewById(R.id.image);
        image.setImageResource(checkIDs.contains(videoFile.videoFileID) ? R.drawable.check : R.drawable.uncheck);
        ((TextView) view.findViewById(R.id.name)).setText(videoFile.title);
        View.OnClickListener clickListener = new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.queueVideo(videoFile.videoFileID, false);
            }
        };
        image.setOnClickListener(clickListener);
        view.setOnClickListener(clickListener);

        View.OnLongClickListener onLongClickListener = new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                mainActivity.queueVideo(videoFile.videoFileID, true);
                return true;
            }
        };
        image.setOnLongClickListener(onLongClickListener);
        view.setOnLongClickListener(onLongClickListener);

        return view;
    }

    @Override
    public void notifyDataSetChanged() {
        displayList.clear();

        for (int videoFileID : showIDs) {
            VideoFile videoFile = videoFiles.get(videoFileID);
            if (videoFile != null)
                displayList.add(videoFile);
        }

        super.notifyDataSetChanged();
    }
}
