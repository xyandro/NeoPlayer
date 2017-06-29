package neoplayer.neoremote;

import android.app.Fragment;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.ImageButton;
import android.widget.ListView;

import java.util.ArrayList;

public class VideosFragment extends Fragment {
    private static final String TAG = MainActivity.class.getSimpleName();

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        View result = inflater.inflate(R.layout.fragment_videos, container, false);

        ListView listView = result.findViewById(R.id.videosList);
        ArrayList<String> listItems = new ArrayList<String>();
        listItems.add("Randon");
        listItems.add("Ben");
        listItems.add("Sophie");
        listItems.add("Timothy");
        listItems.add("Katelyn");
        listItems.add("Phoebe");
        listItems.add("Megan");
        listItems.add("Randon");
        listItems.add("Ben");
        listItems.add("Sophie");
        listItems.add("Timothy");
        listItems.add("Katelyn");
        listItems.add("Phoebe");
        listItems.add("Megan");
        listItems.add("Randon");
        listItems.add("Ben");
        listItems.add("Sophie");
        listItems.add("Timothy");
        listItems.add("Katelyn");
        listItems.add("Phoebe");
        listItems.add("Megan");
        listItems.add("Randon");
        listItems.add("Ben");
        listItems.add("Sophie");
        listItems.add("Timothy");
        listItems.add("Katelyn");
        listItems.add("Phoebe");
        listItems.add("Megan");
        listItems.add("Randon");
        listItems.add("Ben");
        listItems.add("Sophie");
        listItems.add("Timothy");
        listItems.add("Katelyn");
        listItems.add("Phoebe");
        listItems.add("Megan");
        listItems.add("Randon");
        listItems.add("Ben");
        listItems.add("Sophie");
        listItems.add("Timothy");
        listItems.add("Katelyn");
        listItems.add("Phoebe");
        listItems.add("Megan");
        final ArrayAdapter<String> adapter = new ArrayAdapter<String>(getContext(), android.R.layout.simple_list_item_1, listItems);
        listView.setAdapter(adapter);

        return result;
    }
}
