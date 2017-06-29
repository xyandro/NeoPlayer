package neoplayer.neoremote;

import android.app.Activity;
import android.app.Fragment;
import android.content.Context;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.EditText;
import android.widget.Filter;
import android.widget.Filterable;
import android.widget.ImageView;
import android.widget.ListView;
import android.widget.TextView;

import java.util.ArrayList;

public class VideosFragment extends Fragment implements TextWatcher {
    private final ArrayList<VideoData> mAllItems;
    private final VideosListAdapter mAdapter;

    public VideosFragment(Activity activity) {
        mAllItems = new ArrayList<>();
        mAllItems.add(new VideoData("Randon Spackman", "Randon", false));
        mAllItems.add(new VideoData("Ben Christensen", "Ben", true));
        mAllItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        mAllItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        mAllItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        mAllItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        mAllItems.add(new VideoData("Megan Spackman", "Megan", true));
        mAllItems.add(new VideoData("Randon Spackman", "Randon", false));
        mAllItems.add(new VideoData("Ben Christensen", "Ben", true));
        mAllItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        mAllItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        mAllItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        mAllItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        mAllItems.add(new VideoData("Megan Spackman", "Megan", true));
        mAllItems.add(new VideoData("Randon Spackman", "Randon", false));
        mAllItems.add(new VideoData("Ben Christensen", "Ben", true));
        mAllItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        mAllItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        mAllItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        mAllItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        mAllItems.add(new VideoData("Megan Spackman", "Megan", true));
        mAllItems.add(new VideoData("Randon Spackman", "Randon", false));
        mAllItems.add(new VideoData("Ben Christensen", "Ben", true));
        mAllItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        mAllItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        mAllItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        mAllItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        mAllItems.add(new VideoData("Megan Spackman", "Megan", true));
        mAllItems.add(new VideoData("Randon Spackman", "Randon", false));
        mAllItems.add(new VideoData("Ben Christensen", "Ben", true));
        mAllItems.add(new VideoData("Sophie Christensen", "Sophie", true));
        mAllItems.add(new VideoData("Timothy Christensen", "Timothy", false));
        mAllItems.add(new VideoData("Katelyn Spackman", "Katelyn", true));
        mAllItems.add(new VideoData("Phoebe Christensen", "Phoebe", true));
        mAllItems.add(new VideoData("Megan Spackman", "Megan", true));
        mAdapter = new VideosListAdapter(activity, mAllItems);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        View result = inflater.inflate(R.layout.fragment_videos, container, false);

        final EditText editText = result.findViewById(R.id.search_text);
        editText.addTextChangedListener(this);

        ListView listView = result.findViewById(R.id.videosList);

        listView.setAdapter(mAdapter);

        return result;
    }

    @Override
    public void beforeTextChanged(CharSequence charSequence, int i, int i1, int i2) {
    }

    @Override
    public void onTextChanged(CharSequence charSequence, int i, int i1, int i2) {
        mAdapter.getFilter().filter(charSequence);
    }

    @Override
    public void afterTextChanged(Editable editable) {
    }

    private class VideosListAdapter extends BaseAdapter implements Filterable {
        private final Activity activity;
        private final ArrayList<VideoData> mFullList;
        private ArrayList<VideoData> mFilteredList;

        public VideosListAdapter(Activity activity, ArrayList<VideoData> list) {
            super();
            this.activity = activity;
            mFullList = mFilteredList = list;
        }

        @Override
        public int getCount() {
            return mFilteredList.size();
        }

        @Override
        public Object getItem(int i) {
            return mFilteredList.get(i);
        }

        @Override
        public long getItemId(int i) {
            return i;
        }

        @Override
        public View getView(int position, View convertView, ViewGroup parent) {
            LayoutInflater inflater = (LayoutInflater) activity.getLayoutInflater();
            View rowView = inflater.inflate(R.layout.fragment_videos_listitem, parent, false);

            VideoData videoData = mFilteredList.get(position);

            ImageView imageView = rowView.findViewById(R.id.image);
            imageView.setImageResource(videoData.Selected ? R.drawable.check : R.drawable.uncheck);

            TextView textView = rowView.findViewById(R.id.name);
            textView.setText(videoData.Description);

            return rowView;
        }

        @Override
        public Filter getFilter() {
            return new Filter() {
                @Override
                protected FilterResults performFiltering(CharSequence search) {
                    String find = search.toString().toLowerCase();
                    FilterResults result = new FilterResults();
                    if (search.length() == 0) {
                        result.values = mAllItems;
                        result.count = mAllItems.size();
                    } else {
                        final ArrayList<VideoData> items = new ArrayList<>();
                        for (VideoData d : mFullList) {
                            if (d.Description.toLowerCase().contains(find))
                                items.add(d);
                        }
                        result.values = items;
                        result.count = items.size();
                    }

                    return result;
                }

                @Override
                protected void publishResults(CharSequence charSequence, FilterResults results) {
                    mFilteredList = (ArrayList<VideoData>) results.values;
                    notifyDataSetChanged();
                }
            };
        }
    }
}
