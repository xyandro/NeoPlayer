package neoplayer.neoremote;

import android.app.Activity;
import android.app.Fragment;
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

public class VideosFragment extends Fragment {
    private final VideosListAdapter mAdapter;
    private final ArrayList<MediaData> mediaData;

    public VideosFragment(Activity activity, ArrayList<MediaData> mediaData) {
        this.mediaData = mediaData;
        mAdapter = new VideosListAdapter(activity, mediaData);
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
                mAdapter.getFilter().filter(charSequence);
            }

            @Override
            public void afterTextChanged(Editable editable) {
            }
        });

        ((ListView) result.findViewById(R.id.videos_list)).setAdapter(mAdapter);
        result.findViewById(R.id.clear_search).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                searchText.setText("");
            }
        });

        return result;
    }

    public void Refresh() {
        mAdapter.notifyDataSetChanged();
    }

    private class VideosListAdapter extends BaseAdapter implements Filterable {
        private final Activity activity;
        private final ArrayList<MediaData> mFullList;
        private ArrayList<MediaData> mFilteredList;

        public VideosListAdapter(Activity activity, ArrayList<MediaData> list) {
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
            View view = activity.getLayoutInflater().inflate(R.layout.fragment_videos_listitem, parent, false);

            MediaData mediaData = mFilteredList.get(position);

            ImageView imageView = view.findViewById(R.id.image);
            //imageView.setImageResource(mediaData.Selected ? R.drawable.check : R.drawable.uncheck);
            imageView.setImageResource(R.drawable.check);

            TextView textView = view.findViewById(R.id.name);
            textView.setText(mediaData.Description);

            return view;
        }

        @Override
        public Filter getFilter() {
            return new Filter() {
                @Override
                protected FilterResults performFiltering(CharSequence search) {
                    String find = search.toString().toLowerCase();
                    FilterResults result = new FilterResults();
                    if (search.length() == 0) {
                        result.values = mFullList;
                        result.count = mFullList.size();
                    } else {
                        final ArrayList<MediaData> items = new ArrayList<>();
                        for (MediaData mediaData : mFullList) {
                            if (mediaData.Description.toLowerCase().contains(find))
                                items.add(mediaData);
                        }
                        result.values = items;
                        result.count = items.size();
                    }

                    return result;
                }

                @Override
                protected void publishResults(CharSequence charSequence, FilterResults results) {
                    mFilteredList = (ArrayList<MediaData>) results.values;
                    notifyDataSetChanged();
                }
            };
        }
    }
}
