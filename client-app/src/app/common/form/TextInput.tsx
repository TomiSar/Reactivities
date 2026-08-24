import { useField } from 'formik';
import { Form, Label } from 'semantic-ui-react';

interface Props {
    placeholder: string;
    name: string;
    label?: string;
    type?: string;
    rows?: number;
}

export default function TextInput(props: Props) {
    const [field, meta] = useField(props.name);
    const Element = props.rows ? 'textarea' : 'input';
    return (
        <Form.Field error={meta.touched && !!meta.error}>
            <label>{props.label}</label>
            <Element {...field} {...props} />
            {meta.touched && meta.error ? (
                <Label basic color='red'>
                    {meta.error}
                </Label>
            ) : null}
        </Form.Field>
    );
}
