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
    private final HashSet<Integer> starIDs;
    private HashMap<Integer, VideoFile> videoFiles = new HashMap<>();
    private HashSet<Integer> checkIDs = new HashSet<>();
    private ArrayList<Integer> showIDs = new ArrayList<>();
    private final ArrayList<VideoFile> displayList = new ArrayList<>();

    public VideoFileListAdapter(MainActivity mainActivity, HashSet<Integer> starIDs) {
        super();
        this.mainActivity = mainActivity;
        this.starIDs = starIDs;
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
        ImageView checkImage = view.findViewById(R.id.check_image);
        ImageView starImage = view.findViewById(R.id.star_image);
        checkImage.setImageResource(checkIDs.contains(videoFile.videoFileID) ? R.drawable.check : R.drawable.uncheck);
        starImage.setImageResource(starIDs.contains(videoFile.videoFileID) ? R.drawable.star : 0);
        ((TextView) view.findViewById(R.id.name)).setText(videoFile.getTitle());

        final View.OnLongClickListener onLongClickListener = new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                if (starIDs.contains(videoFile.videoFileID))
                    starIDs.remove(videoFile.videoFileID);
                else
                    starIDs.add(videoFile.videoFileID);
                notifyDataSetChanged();
                return true;
            }
        };
        checkImage.setOnLongClickListener(onLongClickListener);
        starImage.setOnLongClickListener(onLongClickListener);
        view.setOnLongClickListener(onLongClickListener);

        View.OnClickListener clickListener = new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                if (!starIDs.isEmpty())
                    onLongClickListener.onLongClick(view);
                else
                    mainActivity.queueVideo(videoFile.videoFileID, false);
            }
        };
        checkImage.setOnClickListener(clickListener);
        starImage.setOnClickListener(clickListener);
        view.setOnClickListener(clickListener);

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
