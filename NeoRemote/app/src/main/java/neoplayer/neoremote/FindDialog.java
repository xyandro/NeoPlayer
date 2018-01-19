package neoplayer.neoremote;

import android.app.Dialog;
import android.app.DialogFragment;
import android.databinding.DataBindingUtil;
import android.os.Bundle;
import android.support.annotation.Nullable;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;

import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;

import neoplayer.neoremote.databinding.FragmentFindBinding;

public class FindDialog extends DialogFragment {
    private MainActivity mainActivity;
    private FragmentFindBinding binding;
    private HashMap<String, String> tags;

    @Override
    public Dialog onCreateDialog(Bundle savedInstanceState) {
        Dialog dialog = super.onCreateDialog(savedInstanceState);
        dialog.getWindow().requestFeature(Window.FEATURE_NO_TITLE);
        return dialog;
    }

    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, Bundle savedInstanceState) {
        binding = DataBindingUtil.inflate(inflater, R.layout.fragment_find, container, false);

        binding.tagsList.setAdapter(new FindAdapter(mainActivity, tags));
        binding.confirm.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                Iterator<Map.Entry<String, String>> itr = tags.entrySet().iterator();
                while (itr.hasNext()) {
                    Map.Entry<String, String> entry = itr.next();
                    String value = entry.getValue();
                    if ((value == null) || (value.trim().isEmpty()))
                        itr.remove();
                }
                if (tags.isEmpty())
                    tags = null;

                mainActivity.setSearchTags(tags);
                dismiss();
            }
        });
        binding.cancel.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.setSearchTags(null);
                dismiss();
            }
        });

        return binding.getRoot();
    }

    public static FindDialog createDialog(MainActivity mainActivity, HashMap<String, String> tags) {
        FindDialog findDialog = new FindDialog();
        findDialog.mainActivity = mainActivity;
        findDialog.tags = tags;
        return findDialog;
    }
}