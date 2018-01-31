package neoplayer.neoremote;

import android.app.AlertDialog;
import android.app.Dialog;
import android.app.DialogFragment;
import android.content.DialogInterface;
import android.databinding.DataBindingUtil;
import android.os.Bundle;
import android.support.annotation.Nullable;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.widget.EditText;

import neoplayer.neoremote.databinding.EditTagsDialogBinding;

public class EditTagsDialog extends DialogFragment {
    private MainActivity mainActivity;
    private EditTagsDialogBinding binding;
    private EditTagsAdapter editTagsAdapter;
    private EditTags editTags;

    @Override
    public Dialog onCreateDialog(Bundle savedInstanceState) {
        Dialog dialog = super.onCreateDialog(savedInstanceState);
        dialog.getWindow().requestFeature(Window.FEATURE_NO_TITLE);
        return dialog;
    }

    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, Bundle savedInstanceState) {
        binding = DataBindingUtil.inflate(inflater, R.layout.edit_tags_dialog, container, false);

        binding.title.setText("Edit Tags: " + editTags.videoFileIDs.size() + " video" + (editTags.videoFileIDs.size() == 1 ? "" : "s"));
        binding.check.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                mainActivity.queueVideos(editTags.videoFileIDs, false);
            }
        });
        binding.check.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View view) {
                mainActivity.queueVideos(editTags.videoFileIDs, true);
                return true;
            }
        });
        binding.add.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                final EditText input = new EditText(mainActivity);
                input.setId(R.id.add);
                new AlertDialog.Builder(mainActivity)
                        .setIcon(android.R.drawable.ic_dialog_info)
                        .setTitle("Add new tag")
                        .setView(input)
                        .setPositiveButton("OK", new DialogInterface.OnClickListener() {
                            public void onClick(DialogInterface dialog, int which) {
                                String tagName = input.getText().toString();
                                editTags.tags.put(tagName, null);
                                editTagsAdapter.updateKeys();
                            }
                        })
                        .setNegativeButton("Cancel", null)
                        .show();
            }
        });
        binding.delete.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                int count = editTags.videoFileIDs.size();
                new AlertDialog.Builder(mainActivity)
                        .setIcon(android.R.drawable.ic_dialog_alert)
                        .setTitle("Delete videos?")
                        .setMessage("Are you sure you want to delete " + count + " video" + (count == 1 ? "" : "s") + "?")
                        .setPositiveButton("Yes", new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                mainActivity.deleteVideos(editTags.videoFileIDs);
                                dismiss();
                            }
                        })
                        .setNegativeButton("No", null)
                        .show();

            }
        });
        binding.clear.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                editTags.clear();
                editTagsAdapter.notifyDataSetChanged();
            }
        });
        binding.submit.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                editTags.removeNulls();
                mainActivity.editTags(editTags);
                dismiss();
            }
        });
        editTagsAdapter = new EditTagsAdapter(mainActivity, editTags);
        binding.list.setAdapter(editTagsAdapter);

        return binding.getRoot();
    }

    public static EditTagsDialog createDialog(MainActivity mainActivity, EditTags editTags) {
        EditTagsDialog editTagsDialog = new EditTagsDialog();
        editTagsDialog.mainActivity = mainActivity;
        editTagsDialog.editTags = editTags;
        return editTagsDialog;
    }
}