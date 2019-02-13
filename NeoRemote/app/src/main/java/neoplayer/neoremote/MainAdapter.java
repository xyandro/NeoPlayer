package neoplayer.neoremote;

import android.databinding.DataBindingUtil;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.PopupMenu;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashSet;

import neoplayer.neoremote.databinding.MainAdapterItemBinding;

public class MainAdapter extends BaseAdapter {
    private final MainActivity mainActivity;
    private final LinkedHashSet<Integer> starIDs;
    private boolean useStarIDs;
    private HashMap<Integer, VideoFile> videoFiles = new HashMap<>();
    private HashSet<Integer> checkIDs = new HashSet<>();
    private ArrayList<Integer> showIDs = new ArrayList<>();
    private final ArrayList<VideoFile> displayList = new ArrayList<>();

    public MainAdapter(MainActivity mainActivity, LinkedHashSet<Integer> starIDs) {
        super();
        this.mainActivity = mainActivity;
        this.starIDs = starIDs;
    }

    public boolean getUseStarIDs() {
        return useStarIDs;
    }

    public void setUseStarIDs(boolean useStarIDs) {
        this.useStarIDs = useStarIDs;
        if (!useStarIDs)
            starIDs.clear();
        notifyDataSetChanged();
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

    public void clearStarIDs() {
        starIDs.clear();
        notifyDataSetChanged();
    }

    public void starShowIDs() {
        for (int videoFileID : showIDs)
            starIDs.add(videoFileID);
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
    public View getView(int position, View convertView, final ViewGroup parent) {
        final VideoFile videoFile = displayList.get(position);

        MainAdapterItemBinding binding;
        if (convertView == null) {
            binding = DataBindingUtil.inflate(mainActivity.getLayoutInflater(), R.layout.main_adapter_item, parent, false);
            binding.getRoot().setTag(binding);
        } else
            binding = (MainAdapterItemBinding) convertView.getTag();

        binding.check.setImageResource(!checkIDs.contains(videoFile.videoFileID) ? R.drawable.uncheck : videoFile.audioOnly() ? R.drawable.note : R.drawable.check);
        binding.name.setText(videoFile.getTitle());
        binding.star.setImageResource(starIDs.contains(videoFile.videoFileID) ? R.drawable.star : 0);

        binding.getRoot().setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                if (useStarIDs) {
                    if (!starIDs.remove(videoFile.videoFileID))
                        starIDs.add(videoFile.videoFileID);
                    notifyDataSetChanged();
                } else
                    mainActivity.queueVideo(videoFile.videoFileID, "Toggle");
            }
        });

        binding.getRoot().setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                PopupMenu popup = new PopupMenu(parent.getContext(), view);
                popup.setOnMenuItemClickListener(new PopupMenu.OnMenuItemClickListener() {
                    @Override
                    public boolean onMenuItemClick(MenuItem item) {
                        switch (item.getItemId()) {
                            case R.id.play_next:
                                mainActivity.queueVideo(videoFile.videoFileID, "PlayNext");
                                return true;
                            case R.id.audio_only:
                                mainActivity.queueVideo(videoFile.videoFileID, "AudioOnly");
                                return true;
                            case R.id.video_and_audio:
                                mainActivity.queueVideo(videoFile.videoFileID, "VideoAndAudio");
                                return true;
                            case R.id.delete:
                                ArrayList<Integer> videoFileIDs = new ArrayList<Integer>();
                                videoFileIDs.add(videoFile.videoFileID);
                                mainActivity.deleteVideos(videoFileIDs);
                                return true;
                            default:
                                return false;
                        }
                    }
                });
                popup.inflate(R.menu.main_adapter_menu);
                popup.show();
                return true;
            }
        });

        return binding.getRoot();
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
