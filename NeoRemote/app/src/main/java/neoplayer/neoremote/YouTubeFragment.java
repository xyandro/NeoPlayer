package neoplayer.neoremote;

import android.app.Fragment;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.ListView;

import java.util.ArrayList;

public class YouTubeFragment extends Fragment {
    private static final String TAG = YouTubeFragment.class.getSimpleName();

    private final MainActivity mainActivity;
    private final MediaListAdapter adapter;
    private final ArrayList<MediaData> youTubeVideos;
    private final ArrayList<MediaData> queueVideos;

    public YouTubeFragment(MainActivity mainActivity, ArrayList<MediaData> youTubeVideos, ArrayList<MediaData> queueVideos) {
        this.mainActivity = mainActivity;
        this.youTubeVideos = youTubeVideos;
        this.queueVideos = queueVideos;
        adapter = new MediaListAdapter(mainActivity, youTubeVideos, queueVideos);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        View result = inflater.inflate(R.layout.fragment_youtube, container, false);

        final EditText searchText = result.findViewById(R.id.search_text);

        ((ListView) result.findViewById(R.id.videos_list)).setAdapter(adapter);
        result.findViewById(R.id.do_search).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.searchYouTube(searchText.getText().toString());
            }
        });

        return result;
    }

    public void Refresh() {
        adapter.notifyDataSetChanged();
    }
}
