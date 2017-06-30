package neoplayer.neoremote;

import android.app.Activity;
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

public class QueuedFragment extends Fragment {
    private static final String TAG = QueuedFragment.class.getSimpleName();

    private final MediaListAdapter adapter;
    private final ArrayList<MediaData> queuedVideos;

    public QueuedFragment(Activity activity, ArrayList<MediaData> queuedVideos) {
        this.queuedVideos = queuedVideos;
        adapter = new MediaListAdapter(activity, queuedVideos, queuedVideos);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        View result = inflater.inflate(R.layout.fragment_videos, container, false);

        final EditText searchText = result.findViewById(R.id.search_text);
        searchText.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
            }

            @Override
            public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
                adapter.setFilter(charSequence.toString());
            }

            @Override
            public void afterTextChanged(Editable editable) {
            }
        });

        ((ListView) result.findViewById(R.id.videos_list)).setAdapter(adapter);
        result.findViewById(R.id.clear_search).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                searchText.setText("");
            }
        });

        return result;
    }

    public void Refresh() {
        adapter.notifyDataSetChanged();
    }
}
