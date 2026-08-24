import { Form, Formik } from 'formik';
import { observer } from 'mobx-react-lite';
import { Button } from 'semantic-ui-react';
import * as Yup from 'yup';
import TextInput from '../../app/common/form/TextInput';
import { useStore } from '../../app/stores/store';

interface Props {
    setEditMode: (editMode: boolean) => void;
}

export default observer(function ProfileEditForm({ setEditMode }: Props) {
    const {
        profileStore: { profile, updateProfile },
    } = useStore();
    return (
        <Formik
            initialValues={{
                displayName: profile?.displayName,
                bio: profile?.bio || '',
            }}
            onSubmit={(values) => {
                updateProfile(values).then(() => {
                    setEditMode(false);
                });
            }}
            validationSchema={Yup.object({
                displayName: Yup.string().required(),
            })}
        >
            {({ isSubmitting, isValid, dirty }) => (
                <Form className='ui form'>
                    <TextInput placeholder='Display Name' name='displayName' />
                    <TextInput rows={3} placeholder='Add your bio' name='bio' />
                    <Button
                        positive
                        type='submit'
                        loading={isSubmitting}
                        content='Update profile'
                        floated='right'
                        disabled={!isValid || !dirty}
                    />
                </Form>
            )}
        </Formik>
    );
});
