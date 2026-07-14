import { Config } from 'dompurify';
declare const sanitize: {
    (dirty: string | Node, cfg: Config & {
        RETURN_TRUSTED_TYPE: true;
    }): import('trusted-types/lib').TrustedHTML;
    (dirty: Node, cfg: Config & {
        IN_PLACE: true;
    }): Node;
    (dirty: string | Node, cfg: Config & {
        RETURN_DOM: true;
    }): Node;
    (dirty: string | Node, cfg: Config & {
        RETURN_DOM_FRAGMENT: true;
    }): DocumentFragment;
    (dirty: string | Node, cfg?: Config): string;
}, isValidAttribute: (tag: string, attr: string, value: string) => boolean;
export { Config, isValidAttribute };
export default sanitize;
